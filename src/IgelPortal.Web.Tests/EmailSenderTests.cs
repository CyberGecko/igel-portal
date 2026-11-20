using System.Net;
using System.Text.Json;
using IgelPortal.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgelPortal.Web.Tests;

[TestClass]
public class EmailSenderTests
{
    [TestMethod]
    public async Task DisabledEmailDoesNotCallBrevo()
    {
        using var client = CreateClient((_, _) => throw new AssertFailedException("Disabled email made an HTTP request."));
        await CreateSender(client, sendMail: false).SendEmailAsync("recipient@example.invalid", "Subject", "<p>Body</p>");
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
            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        await CreateSender(client).SendEmailAsync("recipient@example.invalid", "Subject", "<p>Body</p>");

        Assert.IsTrue(sent);
    }

    [TestMethod]
    public async Task BrevoRejectionIsReportedToTheCaller()
    {
        using var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var error = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CreateSender(client).SendEmailAsync("recipient@example.invalid", "Subject", "<p>Body</p>"));

        Assert.AreEqual(HttpStatusCode.TooManyRequests, error.StatusCode);
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
