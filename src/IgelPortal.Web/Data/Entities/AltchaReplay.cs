namespace IgelPortal.Web.Data.Entities;

public sealed class AltchaReplay
{
    public required string Id { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
