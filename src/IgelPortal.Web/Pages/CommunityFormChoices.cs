using System.Security.Principal;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;

namespace IgelPortal.Web.Pages;

internal static class CommunityFormChoices
{
    internal static async Task<(IList<Community> Options, Guid? Selected)> LoadAsync(
        ICommunityDataService communities, IIdentity? user, string? filter)
    {
        var options = (await communities.FindAllForUserWithRole(user, CommunityRoles.MEMBER)).ToList();
        if (!Guid.TryParse(filter, out var groupId))
        {
            return (options, null);
        }

        if (options.Any(group => group.Id == groupId))
        {
            return (options, groupId);
        }

        // Owners and moderators may be members of older groups without a separate member role.
        if (await communities.IsCommunityMember(groupId, user) &&
            await communities.FindOne(groupId) is { IsDeleted: false } group)
        {
            options.Add(group);
            return (options, groupId);
        }

        return (options, null);
    }
}
