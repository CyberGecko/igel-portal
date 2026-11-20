using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace IgelPortal.Web.Services
{
    public class EmailSender(IOptions<SmtpOptions> smtpOptionAccessor) : IEmailSender
    {
        private readonly SmtpOptions smtpOption = smtpOptionAccessor.Value;

        public async Task SendEmailAsync(String toEmail, String subject, String message)
        {
            if (String.IsNullOrEmpty(toEmail))
            {
                throw new ArgumentNullException(nameof(toEmail));
            }
            if (String.IsNullOrEmpty(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }
            if (String.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            await Execute(toEmail, subject, message);
        }

        public async Task Execute(String toEmail, String subject, String message)
        {
            MimeMessage mailMessage = EmailSender.CreateMailItem(subject, message, toEmail);

            SmtpClient smtp = new();
            await smtp.ConnectAsync(this.smtpOption.Host, this.smtpOption.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(this.smtpOption.Username, this.smtpOption.Password);
            await smtp.SendAsync(mailMessage);
            await smtp.DisconnectAsync(true);
        }


        private static MimeMessage CreateMailItem(String subject, String content, String receiverAddress)
        {
            MimeMessage message = new()
            {
                Sender = new MailboxAddress("Igel Portal", "info@igel-portal.de")
            };
            message.From.Add(message.Sender);
            message.To.Add(new MailboxAddress(receiverAddress, receiverAddress));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = content };


            return message;
        }
    }
}
