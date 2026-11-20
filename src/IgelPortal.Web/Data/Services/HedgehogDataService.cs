using IgelPortal.Web.Data.Models.Mapping;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Services
{
    public class HedgehogDataService(ApplicationDbContext dbContext) : IHedgehogDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        public Task<IList<Hedgehog>> FindAll() => ReadAsync(dbContext.Hedgehogs.Where(entity => entity.DeletedDate == null));

        public Task<IList<Hedgehog>> FindAllActiveForUser(string? userName) =>
            ReadAsync(dbContext.Hedgehogs.Where(entity => entity.DeletedDate == null && entity.AttachedUser == userName));

        public Task<IList<Hedgehog>> FindAllActiveForCommunity(Guid? communityId) =>
            ReadAsync(dbContext.Hedgehogs.Where(entity => entity.DeletedDate == null && entity.CommunityId == communityId));

        public async Task<IList<Hedgehog>> Query(Func<Hedgehog, bool> predicate) =>
            (await ReadAsync(dbContext.Hedgehogs)).Where(predicate).ToList();

        public async Task<Hedgehog?> FindOne(Guid? id) =>
            id.HasValue ? (await ReadAsync(dbContext.Hedgehogs.Where(entity => entity.Id == id))).SingleOrDefault() : null;

        private async Task<IList<Hedgehog>> ReadAsync(IQueryable<Entities.Hedgehog> query)
        {
            var rows = await query.AsNoTracking()
                .OrderBy(entity => entity.Name)
                .Select(entity => new
                {
                    Entity = entity,
                    Home = entity.HomeTerritory != null ? entity.HomeTerritory.Name : null,
                    Current = entity.CurrentTerritory != null ? entity.CurrentTerritory.Name : null,
                    Community = entity.Community != null ? entity.Community.Name : null,
                    Service = entity.UsedService != null && entity.UsedService.TerritoryService != null ? entity.UsedService.TerritoryService.Name : null,
                    OpenRequests = entity.ServiceRequests.Count(request => request.FulfillmentDate == null && request.DeletedDate == null)
                }).ToListAsync();
            var displayNames = await UserDisplayNames.LoadAsync(dbContext, rows.Select(row => row.Entity.AttachedUser));
            return rows.Select(row => HedgehogMapping.Map(row.Entity, row.Home, row.Current, row.Community, row.Service,
                row.OpenRequests, row.Entity.AttachedUser is { } userName ? displayNames.GetValueOrDefault(userName) : null)).ToList();
        }

        public async Task<Guid> Insert(Models.Hedgehog model)
        {
            Entities.Hedgehog entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = model.Name,
                BiologicalSex = model.BiologicalSex,
                AttachedUser = model.AttachedUser,
                CommunityId = model.CommunityId,
                CurrentTerritoryId = model.CurrentTerritoryId,
                HomeTerritoryId = model.HomeTerritoryId,
                UsedServiceId = model.UsedServiceId
            };
            entity.ChangeDate = entity.CreationDate;
            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
            return entity.Id;
        }
        public async Task<Int32> Update(Models.Hedgehog model)
        {
            try
            {
                Entities.Hedgehog? entity = await this.dbContext.Hedgehogs.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }
                entity.ChangeDate = DateTime.UtcNow;
                entity.Name = model.Name;
                entity.BiologicalSex = model.BiologicalSex;
                entity.AttachedUser = model.AttachedUser;
                entity.CommunityId = model.CommunityId;
                entity.CurrentTerritoryId = model.CurrentTerritoryId;
                entity.HomeTerritoryId = model.HomeTerritoryId;
                entity.UsedServiceId = model.UsedServiceId;

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
                Entities.Hedgehog? entity = await this.dbContext.Hedgehogs.FirstOrDefaultAsync(x => x.Id == id);
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
    }
}
