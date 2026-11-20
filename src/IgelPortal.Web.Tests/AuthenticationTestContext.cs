using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace IgelPortal.Web.Tests;

internal sealed class AuthenticationTestContext : IDisposable
{
    internal static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider services;

    public DefaultHttpContext Http { get; } = new();
    public TestAuthenticationService Authentication { get; }

    public AuthenticationTestContext(AuthenticateResult? result = null, Action<IServiceCollection>? configure = null)
    {
        Authentication = new TestAuthenticationService(result ?? AuthenticateResult.NoResult());
        var serviceCollection = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider())
            .AddSingleton<IAuthenticationService>(Authentication);
        configure?.Invoke(serviceCollection);
        services = serviceCollection.BuildServiceProvider();
        Http.RequestServices = services;
    }

    public static AuthenticateResult Authenticated(string scheme, AuthenticationProperties properties, params Claim[] claims) =>
        AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)), properties, scheme));

    public void Dispose() => services.Dispose();

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    internal sealed class TestAuthenticationService(AuthenticateResult result) : IAuthenticationService
    {
        public AuthenticationProperties? SignInProperties { get; private set; }
        public string? RequestedScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            RequestedScheme = scheme;
            return Task.FromResult(result);
        }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignInProperties = properties;
            return Task.CompletedTask;
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();
    }
}
