using Altcha;
using Altcha.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using ModelMapping = IgelPortal.Web.Data.Models.Mapping;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration["Syncfusion:LicenseKey"] is { Length: > 0 } syncfusionLicense)
{
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
}
// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var serverVersion = new Lazy<ServerVersion>(() => ServerVersion.AutoDetect(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, serverVersion.Value,
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
builder.Services.AddScoped<IAuthorizationHandler, CommunityOwnerOrModeratorAuthorizationHandler>();

builder.Services.AddOptions<MailServiceOptions>()
    .BindConfiguration("MailService")
    .Validate(options => !options.SendMail ||
        (!string.IsNullOrWhiteSpace(options.Brevo.ApiKey) &&
         options.Brevo.ApiKey.All(character => !char.IsWhiteSpace(character) && !char.IsControl(character)) &&
         !string.IsNullOrWhiteSpace(options.Sender.Name) &&
         MailAddress.TryCreate(options.Sender.Address, out var address) &&
         address.Address == options.Sender.Address),
        "MailService requires a Brevo API key and a sender name and address when enabled.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IEmailSender, EmailSender>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
.RedactLoggedHeaders(["api-key"]);

builder.Services.AddScoped<ModelMapping.PublicKeyCredentialMapping>();

var captchaSecret = builder.Configuration["RegistrationCaptcha:HmacSecret"];
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(captchaSecret))
{
    captchaSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
builder.Services.AddAltcha(options =>
{
    options.HmacSignatureSecret = captchaSecret;
    options.HmacKeySignatureSecret = string.IsNullOrWhiteSpace(captchaSecret)
        ? null : AltchaSecrets.DeriveHmacKeySecret(captchaSecret);
    options.Challenge.Expires = TimeSpan.FromMinutes(5);
    options.ConfigureChallenge = (context, challenge) => challenge.Data = RegistrationCaptcha.CreateChallengeData(context);
});
builder.Services.AddOptions<AltchaOptions>()
    .Validate(options => options.HmacSignatureSecret is { Length: >= 32 } secret && !string.IsNullOrWhiteSpace(secret),
        "Set RegistrationCaptcha:HmacSecret to at least 32 random characters.")
    .ValidateOnStart();
builder.Services.Replace(ServiceDescriptor.Singleton<IAltchaReplayStore, DatabaseAltchaReplayStore>());
builder.Services.AddScoped<RegistrationCaptcha>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RegistrationCaptcha.ChallengeRateLimit, context =>
        RegistrationLimiter(context, 20));
    options.AddPolicy(RegistrationCaptcha.SubmitRateLimit, context =>
        HttpMethods.IsPost(context.Request.Method)
            ? RegistrationLimiter(context, 5)
            : RateLimitPartition.GetNoLimiter(""));
});

builder.Services.AddDistributedMemoryCache();
if (builder.Configuration["DataProtection:KeyRingPath"] is { Length: > 0 } keyRingPath)
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
}

builder.Services.AddFido2(options =>
{
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>();
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"];
    options.ServerName = builder.Configuration["Fido2:ServerName"];
    options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("Fido2:TimestampDriftTolerance");
});

var googleAuth = builder.Configuration.GetSection("Authentication:Google");
if (!string.IsNullOrWhiteSpace(googleAuth["ClientId"]) && !string.IsNullOrWhiteSpace(googleAuth["ClientSecret"]))
{
    builder.Services.AddAuthentication().AddGoogle("google", options =>
    {
        options.ClientId = googleAuth["ClientId"]!;
        options.ClientSecret = googleAuth["ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var supportedCultures = new[] { "de-DE", "en-GB" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
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
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(3));


var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[]
    {
        new IdentityRole(RoleNames.ADMIN) { Id = "9d263796-50a1-4573-a57c-8afe12834e99" },
        new IdentityRole(RoleNames.USER) { Id = "e1536d79-28f8-4981-990b-fbeb59878669" }
    })
    {
        if (!await roleManager.RoleExistsAsync(role.Name!))
        {
            var seededRole = await roleManager.FindByIdAsync(role.Id);
            IdentityResult result;
            if (seededRole is null)
            {
                result = await roleManager.CreateAsync(role);
            }
            else
            {
                seededRole.Name = role.Name;
                result = await roleManager.UpdateAsync(seededRole);
            }
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(role.Name!))
            {
                throw new InvalidOperationException($"Could not initialize role {role.Name}.");
            }
        }
    }
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next(context);
});
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();
app.MapGet("/registration-captcha", (HttpContext context, RegistrationCaptcha captcha) => captcha.IssueChallenge(context))
    .AllowAnonymous()
    .RequireRateLimiting(RegistrationCaptcha.ChallengeRateLimit);
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.MapPost("/fido2/createassertion", Fido2Handler.CreateAssertion);
app.MapPost("/fido2/createassertionoptions", Fido2Handler.CreateAssertionOptions);
app.MapPost("/fido2/createattestation", Fido2Handler.CreateAttestation).RequireAuthorization();
app.MapPost("/fido2/createattestationoptions", Fido2Handler.CreateAttestationOptions).RequireAuthorization();

app.Run();


static RateLimitPartition<string> RegistrationLimiter(HttpContext context, int permitLimit)
{
    var address = context.Connection.RemoteIpAddress;
    if (address?.IsIPv4MappedToIPv6 == true)
    {
        address = address.MapToIPv4();
    }
    return RateLimitPartition.GetFixedWindowLimiter(address?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    });
}
