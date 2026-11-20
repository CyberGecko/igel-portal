using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Services;

internal static class UserDisplayNames
{
    public static async Task<Dictionary<string, string>> LoadAsync(ApplicationDbContext dbContext, IEnumerable<string?> userNames)
    {
        var names = userNames.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length == 0)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var users = await dbContext.Users.AsNoTracking()
            .Where(user => user.UserName != null && names.Contains(user.UserName))
            .ToListAsync();
        return users.ToDictionary(user => user.UserName!, user => user.ToString(), StringComparer.OrdinalIgnoreCase);
    }
}
