using Microsoft.AspNetCore.Mvc.Rendering;

namespace IgelPortal.Web.Models
{
    public class CommunityAdministrationMenuModel
    {
        public IList<Data.Models.Community> AdministratedCommunities { get; set; } = default!;
        public SelectList JoinableCommunities { get; set; } = default!;
        public IList<Data.Models.Community> JoinedCommunities { get; set; } = default!;
    }

}
