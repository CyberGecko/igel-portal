using AutoMapper;
using IgelPortal.Web.Extensions;

namespace IgelPortal.Web.Data.Models.Mapping
{
    public class TerritoryServiceMapping : Profile
    {
        public TerritoryServiceMapping()
        {
            CreateMap<Entities.TerritoryService, TerritoryService>()
                .BaseMapping();
        }
    }

}
