using System.Globalization;
using System.Net;
using System.Text.Json;
using IgelPortal.Web.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgelPortal.Web.Tests;

[TestClass]
public class EmailSenderTests
{
    private static readonly EmailContent Example = new("Subject", "<p>Body</p>", "Body");

    [TestMethod]
    public async Task DisabledEmailDoesNotCallBrevo()
    {
        using var client = CreateClient((_, _) => throw new AssertFailedException("Disabled email made an HTTP request."));
        await CreateSender(client, sendMail: false).SendEmailAsync("recipient@example.invalid", Example);
    }

    [TestMethod]
    public async Task SendEmailPostsTheExpectedTransactionalMessage()
    {
        var sent = false;
        using var client = CreateClient(async (request, cancellationToken) =>
        {
            sent = true;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v3/smtp/email", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("test-key", request.Headers.GetValues("api-key").Single());

            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var message = body.RootElement;
            Assert.AreEqual("sender@example.invalid", message.GetProperty("sender").GetProperty("email").GetString());
            Assert.AreEqual("recipient@example.invalid", message.GetProperty("to")[0].GetProperty("email").GetString());
            Assert.AreEqual("Subject", message.GetProperty("subject").GetString());
            Assert.AreEqual("<p>Body</p>", message.GetProperty("htmlContent").GetString());
            Assert.AreEqual("Body", message.GetProperty("textContent").GetString());
            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        await CreateSender(client).SendEmailAsync("recipient@example.invalid", Example);

        Assert.IsTrue(sent);
    }

    [TestMethod]
    public async Task BrevoRejectionIsReportedToTheCaller()
    {
        using var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CreateSender(client).SendEmailAsync("recipient@example.invalid", Example));

        Assert.AreEqual(HttpStatusCode.TooManyRequests, error.StatusCode);
    }

    [TestMethod]
    [DataRow("de-DE", "Neues Passwort festlegen", "Setze dein Passwort zurück")]
    [DataRow("en-GB", "Set a new password", "Reset your password")]
    public async Task PasswordResetIncludesALocalizedButtonAndTheCompleteLink(string culture, string action, string title)
    {
        const string url = "https://igel-portal.example.invalid/Identity/Account/ResetPassword?code=test&source=email";
        using var client = CreateClient(async (request, token) =>
        {
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var message = document.RootElement;
            Assert.AreEqual($"IgelPortal – {title}", message.GetProperty("subject").GetString());
            var html = message.GetProperty("htmlContent").GetString()!;
            StringAssert.Contains(html, $"lang=\"{culture}\"");
            StringAssert.Contains(html, action);
            StringAssert.Contains(html, "href=\"https://igel-portal.example.invalid/Identity/Account/ResetPassword?code=test&amp;source=email\"");
            StringAssert.Contains(message.GetProperty("textContent").GetString(), $"{action}\n{url}");
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            await MakePortalMail(client).ResetPasswordAsync("recipient@example.invalid", url);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public void LayoutTreatsMessageAndLinkAsText()
    {
        using var client = CreateClient((_, _) => throw new AssertFailedException("Rendering made an HTTP request."));
        var mail = MakePortalMail(client);
        const string url = "https://igel-portal.example.invalid/path?a=1&b=2";
        var message = mail.Render("Request", "<strong>Igel & Gruppe</strong>", url, "Open", null);

        StringAssert.Contains(message.Html, "&lt;strong&gt;Igel &amp; Gruppe&lt;/strong&gt;");
        StringAssert.Contains(message.Html, "<a href=\"https://igel-portal.example.invalid/path?a=1&amp;b=2\"");
        StringAssert.Contains(message.PlainText, "<strong>Igel & Gruppe</strong>");
        StringAssert.Contains(message.PlainText, url);
        Assert.ThrowsExactly<ArgumentException>(() => mail.Render("Request", "Message", "/relative", "Open", null));
    }

    [TestMethod]
    public async Task EachNotificationUsesTheSharedLayoutAndAnActionLink()
    {
        const string email = "recipient@example.invalid";
        const string link = "https://igel-portal.example.invalid/message";
        var messages = new List<string>();
        using var client = CreateClient(async (request, token) =>
        {
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var message = document.RootElement;
            var html = message.GetProperty("htmlContent").GetString()!;
            StringAssert.Contains(html, "<!doctype html>");
            StringAssert.Contains(html, $"href=\"{link}\"");
            StringAssert.Contains(message.GetProperty("textContent").GetString(), link);
            messages.Add(message.GetProperty("subject").GetString()!);
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var mail = MakePortalMail(client);
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            await mail.ConfirmEmailAsync(email, link);
            await mail.ChangeEmailAsync(email, link);
            await mail.JoinRequestAsync(email, "Robin", "Igelgruppe", link);
            await mail.PlacementRequestAsync(email, "Igelgruppe", "Mila", "Überwinterung", new DateOnly(2026, 12, 1),
                new DateOnly(2027, 2, 1), link);
            await mail.PlacementAssignedAsync(email, "Mila", "Überwinterung", "Wiesenrevier", link);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        CollectionAssert.AreEqual(new[]
        {
            "IgelPortal – Bestätige deine E-Mail-Adresse",
            "IgelPortal – Bestätige deine neue E-Mail-Adresse",
            "IgelPortal – Neue Beitrittsanfrage",
            "IgelPortal – Ein Igel sucht einen Platz",
            "IgelPortal – Dein Platzangebot wurde ausgewählt"
        }, messages.ToArray());
    }

    private static PortalMail MakePortalMail(HttpClient client)
    {
        var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance);
        return new PortalMail(CreateSender(client), new StringLocalizer<PortalMail>(factory));
    }

    private static EmailSender CreateSender(HttpClient client, bool sendMail = true) =>
        new(client, Options.Create(new MailServiceOptions
        {
            SendMail = sendMail,
            Brevo = new() { ApiKey = "test-key" },
            Sender = new() { Address = "sender@example.invalid" }
        }), NullLogger<EmailSender>.Instance);

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
        new(new TestMessageHandler(send)) { BaseAddress = new Uri("https://brevo.example.invalid/") };

    private sealed class TestMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) :
        HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
