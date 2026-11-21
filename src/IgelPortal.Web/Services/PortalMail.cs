using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;

namespace IgelPortal.Web.Services;

public sealed class PortalMail(EmailSender sender, IStringLocalizer<PortalMail> text)
{
    public Task ConfirmEmailAsync(string recipient, string url) =>
        SendAsync(recipient, text["Confirm your email address"], text["Confirm your email address to activate your IgelPortal account."],
            url, text["Confirm email address"], text["If you did not create an account or request this email, you can ignore it."]);

    public Task ChangeEmailAsync(string recipient, string url) =>
        SendAsync(recipient, text["Confirm your new email address"], text["Confirm that you want to use this address for your IgelPortal account."],
            url, text["Confirm new address"], text["If you did not request this change, you can ignore this email."]);

    public Task ResetPasswordAsync(string recipient, string url) =>
        SendAsync(recipient, text["Reset your password"], text["You requested a new password for your IgelPortal account. Use the link below to set it."],
            url, text["Set a new password"], text["If you did not request this, you can ignore this email. Your password stays the same."]);

    public Task JoinRequestAsync(string recipient, string applicant, string group, string url) =>
        SendAsync(recipient, text["New request to join your group"],
            text["{0} would like to join {1}. You can review the request in the member list.", applicant, group],
            url, text["Review join request"]);

    public Task PlacementRequestAsync(string recipient, string group, string hedgehog, string service, DateOnly from,
        DateOnly? until, string url)
    {
        var period = until.HasValue
            ? text["from {0} to {1}", from.ToString("d"), until.Value.ToString("d")]
            : text["from {0}", from.ToString("d")];
        return SendAsync(recipient, text["A hedgehog needs a place"],
            text["{0} is looking for a place for {1} for “{2}” {3}. Let the group know whether you can help.", group, hedgehog, service, period.Value],
            url, text["View request and reply"]);
    }

    public Task PlacementAssignedAsync(string recipient, string hedgehog, string service, string territory, string url) =>
        SendAsync(recipient, text["Your offer was selected"],
            text["Your place at {0} was assigned to {1} for “{2}”. Open the request to review the details.", territory, hedgehog, service],
            url, text["View assignment"]);

    private Task SendAsync(string recipient, string title, string body, string url, string action, string? note = null) =>
        sender.SendEmailAsync(recipient, Render(title, body, url, action, note));

    internal EmailContent Render(string title, string body, string url, string action, string? note)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var link) ||
            (link.Scheme != Uri.UriSchemeHttps && link.Scheme != Uri.UriSchemeHttp) || link.UserInfo.Length != 0)
        {
            throw new ArgumentException("An absolute HTTP or HTTPS action URL is required.", nameof(url));
        }

        var encode = HtmlEncoder.Default;
        var safeUrl = encode.Encode(url);
        var greeting = text["Hello,"].Value;
        var signOff = text["The IgelPortal team"].Value;
        var fallback = text["If the button does not work, use this link."].Value;
        var subject = $"IgelPortal – {title}";
        var noteHtml = note is null ? "" : $"<p style=\"margin:24px 0 0;color:#46534b\">{encode.Encode(note)}</p>";
        var html = $"""
            <!doctype html>
            <html lang="{encode.Encode(CultureInfo.CurrentUICulture.Name)}">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{encode.Encode(subject)}</title></head>
            <body style="margin:0;background:#f3f6f2;color:#203128;font:16px/1.55 Arial,sans-serif">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f3f6f2">
                <tr><td align="center" style="padding:24px 12px">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:580px;background:#fff;border:1px solid #dfe6de">
                    <tr><td style="background:#1e5038;padding:16px 24px;color:#fff;font-size:19px;font-weight:bold">IgelPortal</td></tr>
                    <tr><td align="left" style="padding:24px">
                      <h1 style="margin:0 0 20px;font-size:24px;line-height:1.25">{encode.Encode(title)}</h1>
                      <p>{encode.Encode(greeting)}</p>
                      <p>{encode.Encode(body)}</p>
                      <p style="margin:24px 0"><a href="{safeUrl}" style="display:inline-block;padding:12px 18px;border-radius:6px;background:#1e5038;color:#fff;font-weight:bold;text-decoration:none">{encode.Encode(action)}</a></p>
                      {noteHtml}
                      <p style="margin:24px 0">{encode.Encode(signOff)}</p>
                      <p style="margin:0;padding-top:16px;border-top:1px solid #dfe6de;font-size:14px;color:#46534b;overflow-wrap:anywhere">
                        {encode.Encode(fallback)}<br><a href="{safeUrl}" style="color:#1e5038;word-break:break-all">{safeUrl}</a>
                      </p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        var plain = string.Join("\n\n", new[] { greeting, body, $"{action}\n{url}", note, signOff }.OfType<string>());
        return new(subject, html, plain);
    }
}

public sealed record EmailContent(string Subject, string Html, string PlainText);
