using AutoMapper;
using IgelPortal.Web;
using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Identity;
using IgelPortal.Web.Data.Services;
using IgelPortal.Web.Handler.Fido2;
using IgelPortal.Web.Interfaces;
using IgelPortal.Web.PolicyRequirements;
using IgelPortal.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Nominatim.API.Geocoders;
using Nominatim.API.Interfaces;
using Nominatim.API.Web;
using System.Globalization;
using ModelMapping = IgelPortal.Web.Data.Models.Mapping;

var builder = WebApplication.CreateBuilder(args);

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"]);
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                                                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name).EnableStringComparisonTranslations()));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IHedgehogDataService, HedgehogDataService>();
builder.Services.AddScoped<ITerritoryDataService, TerritoryDataService>();
builder.Services.AddScoped<ICommunityDataService, CommunityDataService>();
builder.Services.AddScoped<ITerritoryServiceDataService, TerritoryServiceDataService>();
builder.Services.AddScoped<ITerritoryAttachedServiceDataService, TerritoryAttachedServiceDataService>();
builder.Services.AddScoped<IServiceRequestDataService, ServiceRequestDataService>();
builder.Services.AddScoped<IFido2DataService, Fido2DataService>();

builder.Services.AddScoped<INominatimWebInterface, NominatimWebInterface>();
builder.Services.AddScoped<IForwardGeocoder, ForwardGeocoder>();

builder.Services.AddScoped<IAuthorizationHandler, CommunityRoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommunityOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CommunityModeratorAuthorizationHandler>();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new ModelMapping.CommunityMapping());
    cfg.AddProfile(new ModelMapping.TerritoryServiceMapping());
    cfg.AddProfile(new ModelMapping.TerritoryAttachedServiceMapping());
    cfg.AddProfile(new ModelMapping.CommunityMemberMapping(provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()));
    cfg.AddProfile(new ModelMapping.HedgehogMapping(provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()));
    cfg.AddProfile(new ModelMapping.TerritoryMapping());
    cfg.AddProfile(new ModelMapping.ServiceRequestMapping());
    cfg.AddProfile(new ModelMapping.ServiceRequestResponseMapping(provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()));
    cfg.AddProfile(new ModelMapping.PublicKeyCredentialMapping(provider.CreateScope().ServiceProvider.GetRequiredService<IStringLocalizer<ModelMapping.PublicKeyCredentialMapping>>()));
}).CreateMapper());


builder.Services.AddDistributedMemoryCache();

builder.Services.AddFido2(options =>
{
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>();
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"];
    options.ServerName = builder.Configuration["Fido2:ServerName"];
    options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("Fido2:TimestampDriftTolerance");
});

builder.Services.AddAuthentication().AddGoogle("google", options =>
{
    var googleAuth = builder.Configuration.GetSection("Authentication:Google");
    if (googleAuth != null)
    {
        options.ClientId = googleAuth["ClientId"] ?? String.Empty;
        options.ClientSecret = googleAuth["ClientSecret"] ?? String.Empty;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    }
});
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var supportedCultures = new[] { "de-DE", "en-GB" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

builder.Services.AddRazorPages()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                    {
                        return new DataAnnotationStringLocalizer(
                            factory?.Create(type),
                            factory?.Create(typeof(SharedModelResource))
                        );
                    };
                });

builder.Services.AddAuthorizationBuilder()
                .AddPolicy(PolicyNames.COMMUNITY_OWNER_OR_MODERATOR, policy =>
                {
                    policy.Requirements.Add(new CommunityOwnerOrModeratorRequirement());
                })
                .AddPolicy(PolicyNames.COMMUNITY_OWNER, policy =>
                {
                    policy.Requirements.Add(new CommunityRoleRequirement(CommunityRoles.OWNER));
                });

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/AccessDenied";
});
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(3));


var app = builder.Build();

using (IServiceScope serviceScope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var pendingMigrations = dbContext.Database.GetPendingMigrations();
    if (pendingMigrations.Any())
    {
        Console.WriteLine("Applying pending migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Migrations applied successfully.");
    }
    else
    {
        Console.WriteLine("No pending migrations found.");
    }

    RoleManager<IdentityRole> roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!roleManager.RoleExistsAsync(RoleNames.ADMIN).Result)
    {
        var adminRole = new IdentityRole(RoleNames.ADMIN)
        {
            Id = "9d263796-50a1-4573-a57c-8afe12834e99"
        };
        roleManager.CreateAsync(adminRole).Wait();

        //dbContext.Add<IdentityRole>(new IdentityRole(RoleNames.ADMIN) { Id = "9d263796-50a1-4573-a57c-8afe12834e99", NormalizedName = RoleNames.ADMIN.ToUpper() });
    }
    if (!roleManager.RoleExistsAsync(RoleNames.USER).Result)
    {
        var userRole = new IdentityRole(RoleNames.USER)
        {
            Id = "e1536d79-28f8-4981-990b-fbeb59878669"
        };
        roleManager.CreateAsync(userRole).Wait();
        //dbContext.Add<IdentityRole>(new IdentityRole(RoleNames.USER) { Id = "e1536d79-28f8-4981-990b-fbeb59878669", NormalizedName = RoleNames.USER.ToUpper() });
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var options = new RequestLocalizationOptions
{

    DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
    SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList(),
    ApplyCurrentCultureToResponseHeaders = true,
    FallBackToParentCultures = true,
    FallBackToParentUICultures = true,
};
app.UseRequestLocalization(options);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.MapPost("/fido2/createassertion", Fido2Handler.CreateAssertion);
app.MapPost("/fido2/createassertionoptions", Fido2Handler.CreateAssertionOptions);
app.MapPost("/fido2/createattestation", Fido2Handler.CreateAttestation);
app.MapPost("/fido2/createattestationoptions", Fido2Handler.CreateAttestationOptions);

app.Run();
