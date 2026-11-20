using Altcha.AspNetCore;
using IgelPortal.Web.Data;
using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Security.Cryptography;
using System.Text;

namespace IgelPortal.Web.Services;

// The primary key makes every successful claim single-use across application instances.
public sealed class DatabaseAltchaReplayStore(IServiceScopeFactory scopeFactory) : IAltchaReplayStore
{
    public async ValueTask<bool> TryClaimAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        await db.AltchaReplays.Where(replay => replay.ExpiresAtUtc <= now).ExecuteDeleteAsync(cancellationToken);
        db.AltchaReplays.Add(new AltchaReplay
        {
            Id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))),
            ExpiresAtUtc = expiresAt.UtcDateTime
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            return false;
        }
    }
}
