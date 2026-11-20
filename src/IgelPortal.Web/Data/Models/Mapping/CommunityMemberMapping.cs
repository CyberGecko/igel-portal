namespace IgelPortal.Web.Data.Models.Mapping;

public static class CommunityMemberMapping
{
    public static CommunityMember Map(Entities.CommunityMember entity) => new()
    {
        UserName = entity.UserName!,
        CommunityId = entity.CommunityId,
        CommunityRoleName = entity.CommunityRoleName!,
        Community = (entity.Community?.Name)!,
        DisplayName = entity.UserName!
    };
}
