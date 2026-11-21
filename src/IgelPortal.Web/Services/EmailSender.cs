using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IgelPortal.Web.Services;

public sealed class EmailSender(HttpClient client, IOptions<MailServiceOptions> options, ILogger<EmailSender> logger)
{
    public async Task SendEmailAsync(string email, EmailContent message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Html);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.PlainText);

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
                subject = message.Subject,
                htmlContent = message.Html,
                textContent = message.PlainText,
                tags = new[] { "igel-portal" }
            })
        };
        request.Headers.Add("api-key", settings.Brevo.ApiKey);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
    }
}
