namespace IgelPortal.Web
{
    public class SmtpOptions
    {
        public String Host { get; set; } = default!;
        public Int32 Port { get; set; }
        public Boolean EnableSsl { get; set; }
        public String Username { get; set; } = default!;
        public String Password { get; set; } = default!;
    }
}
