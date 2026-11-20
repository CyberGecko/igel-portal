using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IgelPortal.Web.Services;

public sealed class EmailSender(HttpClient client, IOptions<MailServiceOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlMessage);

        var settings = options.Value;
        if (!settings.SendMail)
        {
            logger.LogInformation("Transactional email is disabled.");
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = settings.Sender.Name, email = settings.Sender.Address },
                to = new[] { new { email } },
                subject,
                htmlContent = htmlMessage,
                tags = new[] { "igel-portal" }
            })
        };
        request.Headers.Add("api-key", settings.Brevo.ApiKey);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
    }
}
