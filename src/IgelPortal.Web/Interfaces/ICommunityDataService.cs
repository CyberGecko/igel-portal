using System.Security.Principal;
using Entity = IgelPortal.Web.Data.Entities;
using Model = IgelPortal.Web.Data.Models;

namespace IgelPortal.Web.Interfaces
{
    public interface ICommunityDataService : IDataService<Entity.Community, Model.Community>
    {
        Task<IList<Model.Community>> FindAllForUserWithRole(IIdentity? user, String? communityRole);
        Task<IList<Model.CommunityMember>> GetCommunityMembers(Guid? communityId, Boolean includeApplicant);
        Task<bool> IsCommunityMember(Guid? communityId, IIdentity? user);
        Task<Boolean> HasUserCommunityMemberRole(Guid? communityId, IIdentity? user, String? communityRole);
        Task AcceptCommunityApplication(Guid? communityId, String userName);
        Task RejectCommunityApplication(Guid? communityId, String userName);
        Task PromoteUserToCommunityModerator(Guid? communityId, String userName);
        Task DemoteUserFromCommunityModerator(Guid? communityId, String userName);
        Task RemoveUserFromCommunity(Guid? communityId, String userName);
        Task<bool> TryAddUserAsApplicant(Guid communityId, IIdentity? user);
        Task AddUserAsOwner(Guid? communityId, IIdentity? user);
        Task AddUserAsOwner(Guid? communityId, String userName);
        Task AddUserAsMember(Guid? communityId, IIdentity? user);
        Task AddUserAsModerator(Guid? communityId, IIdentity? user);
    }
}
