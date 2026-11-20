using IgelPortal.Web.Data.Models.Mapping;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;
using Nominatim.API.Interfaces;
using Nominatim.API.Models;
using System.Security.Principal;

namespace IgelPortal.Web.Data.Services
{
    public class TerritoryDataService(ApplicationDbContext dbContext, IForwardGeocoder forwardGeocoder) : ITerritoryDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IForwardGeocoder forwardGeocoder = forwardGeocoder ?? throw new ArgumentNullException(nameof(forwardGeocoder));

        public async Task<IList<Models.Territory>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.Territory>> Query(Func<Models.Territory, bool> predicate)
        {
            IEnumerable<Entities.Territory> entities = await this.dbContext.Territories.AsNoTrackingWithIdentityResolution().AsSplitQuery().Include(e => e.HomeHedgehogs)
                                                                                       .Include(e => e.CurrentHedgehogs)
                                                                                       .Include(e => e.Community)
                                                                                       .Include(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                                                       .OrderBy(e => e.Name)
                                                                                       .ToListAsync();

            var models = entities.Select(TerritoryMapping.Map).ToList();

            return models.Where(predicate).ToList();
        }
        public async Task<Models.Territory?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.Territory? entity = await this.dbContext.Territories.Where(e => e.Id == id)
                                                                         .AsNoTrackingWithIdentityResolution().AsSplitQuery().Include(e => e.HomeHedgehogs)
                                                                         .Include(e => e.CurrentHedgehogs)
                                                                         .Include(e => e.Community)
                                                                         .Include(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                                         .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }

            return TerritoryMapping.Map(entity);
        }

        public async Task<Guid> Insert(Models.Territory model)
        {
            Entities.Territory entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = model.Name,
                Street = model.Street,
                StreetNumber = model.StreetNumber,
                PostalCode = model.PostalCode,
                City = model.City,
                CommunityId = model.CommunityId,
                UserName = model.UserName
            };
            GeocodeResponse? geo = await GetLatLonFromAddress(entity);
            entity.Longitude = geo?.Longitude ?? 0;
            entity.Latitude = geo?.Latitude ?? 0;

            entity.ChangeDate = entity.CreationDate;

            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<Int32> Update(Models.Territory model)
        {
            try
            {
                Entities.Territory? entity = await this.dbContext.Territories.FirstOrDefaultAsync(e => e.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }

                Boolean addressChanged = !entity.Street.Equals(model.Street, StringComparison.InvariantCultureIgnoreCase) ||
                                         !entity.StreetNumber.Equals(model.StreetNumber, StringComparison.InvariantCultureIgnoreCase) ||
                                         !entity.PostalCode.Equals(model.PostalCode, StringComparison.InvariantCultureIgnoreCase) ||
                                         !entity.City.Equals(model.City, StringComparison.InvariantCultureIgnoreCase);

                entity.ChangeDate = DateTime.UtcNow;
                entity.Name = model.Name;
                entity.Street = model.Street;
                entity.StreetNumber = model.StreetNumber;
                entity.PostalCode = model.PostalCode;
                entity.City = model.City;
                entity.CommunityId = model.CommunityId;
                entity.UserName = model.UserName;

                if (addressChanged || entity.Longitude == 0 || entity.Latitude == 0)
                {
                    GeocodeResponse? geo = await GetLatLonFromAddress(entity);
                    entity.Longitude = geo?.Longitude ?? 0;
                    entity.Latitude = geo?.Latitude ?? 0;
                }

                return await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return 0;
            }
        }
        public async Task<Int32> Delete(Guid id)
        {
            try
            {
                Entities.Territory? entity = await this.dbContext.Territories.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return 0;
                }
                entity.DeletedDate = DateTime.UtcNow;

                return await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return 0;
            }
        }

        public async Task<IList<Models.Territory>> GetAllAdministratedByUser(IIdentity? identity)
        {
            if (identity == null)
            {
                return [];
            }
            var administratedTerritories = await this.dbContext.CommunityMembers.Where(e => e.UserName == identity.Name &&
                                                                                            (e.CommunityRoleName == CommunityRoles.OWNER ||
                                                                                             e.CommunityRoleName == CommunityRoles.MODERATOR))
                                                                                .Include(e => e.Community)
                                                                                .ThenInclude(e => e.Territories)
                                                                                .SelectMany(e => e.Community.Territories)
                                                                                .Where(e => e.DeletedDate == null)
                                                                                .ToListAsync();
            var ownTerritories = await this.dbContext.Territories.Where(e => e.UserName == identity.Name && e.DeletedDate == null)
                                                    .OrderBy(e => e.Name)
                                                    .ToListAsync();
            administratedTerritories.AddRange(ownTerritories);
            administratedTerritories = administratedTerritories.DistinctBy(e => e.Id).ToList();

            return administratedTerritories.Select(TerritoryMapping.Map).ToList();
        }

        public async Task<Boolean> IsTerritoryDeletableByUser(Guid? territoryId, IIdentity? identity)
        {
            if (identity == null || !territoryId.HasValue)
            {
                return false;
            }
            return await dbContext.Territories.AnyAsync(territory => territory.Id == territoryId &&
                territory.DeletedDate == null && (territory.UserName == identity.Name ||
                    dbContext.CommunityMembers.Any(member => member.CommunityId == territory.CommunityId &&
                        member.UserName == identity.Name && member.CommunityRoleName == CommunityRoles.OWNER &&
                        member.Community.DeletedDate == null)));
        }

        public async Task<IList<Models.Territory>> FindAllForCommunityWithService(Guid? communityId, Guid? serviceId)
        {
            IEnumerable<Entities.Territory> entities = await this.dbContext.Territories.Where(e => e.CommunityId == communityId && e.AttachedServices.Any(c => c.TerritoryServiceId == serviceId))
                                                                                       .AsNoTrackingWithIdentityResolution().AsSplitQuery().Include(e => e.HomeHedgehogs)
                                                                                       .Include(e => e.CurrentHedgehogs)
                                                                                       .Include(e => e.Community)
                                                                                       .Include(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                                                       .OrderBy(e => e.Name)
                                                                                       .ToListAsync();


            return entities.Select(TerritoryMapping.Map).ToList();
        }
        public async Task<IList<Models.Territory>> FindAllActiveForCommunity(Guid? communityId)
        {
            IEnumerable<Entities.Territory> entities = await this.dbContext.Territories.Where(e => e.CommunityId == communityId && !e.DeletedDate.HasValue)
                                                                                       .AsNoTrackingWithIdentityResolution().AsSplitQuery().Include(e => e.HomeHedgehogs)
                                                                                       .Include(e => e.CurrentHedgehogs)
                                                                                       .Include(e => e.Community)
                                                                                       .Include(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                                                       .OrderBy(e => e.Name)
                                                                                       .ToListAsync();

            return entities.Select(TerritoryMapping.Map).ToList();
        }
        public async Task<IList<Models.Territory>> FindAllActiveForUser(IIdentity? identity)
        {
            if (identity == null)
            {
                return [];
            }

            var entities = await this.dbContext.Territories.Where(e => !e.DeletedDate.HasValue && e.UserName == identity.Name)
                                                    .AsNoTrackingWithIdentityResolution().AsSplitQuery().Include(e => e.HomeHedgehogs)
                                                    .Include(e => e.CurrentHedgehogs)
                                                    .Include(e => e.Community)
                                                    .Include(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                    .OrderBy(e => e.Name)
                                                    .ToListAsync();
            return entities.Select(TerritoryMapping.Map).ToList();
        }


        private async Task<GeocodeResponse?> GetLatLonFromAddress(Entities.Territory entity)
        {
            GeocodeResponse[] response = await this.forwardGeocoder.Geocode(new Nominatim.API.Models.ForwardGeocodeRequest()
            {
                StreetAddress = $"{entity.Street} {entity.StreetNumber}",
                City = entity.City,
                PostalCode = entity.PostalCode
            });
            return response?.ElementAtOrDefault(0);
        }
    }
}
