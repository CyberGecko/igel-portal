namespace IgelPortal.Web.Services;

public sealed class MailServiceOptions
{
    public bool SendMail { get; set; }
    public BrevoOptions Brevo { get; set; } = new();
    public SenderOptions Sender { get; set; } = new();

    public sealed class BrevoOptions
    {
        public string ApiKey { get; set; } = "";
    }

    public sealed class SenderOptions
    {
        public string Name { get; set; } = "Igel Portal";
        public string Address { get; set; } = "info@igel-portal.de";
    }
}
