using AutoMapper;
using IgelPortal.Web.Data.Models;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Services
{
    public class HedgehogDataService(ApplicationDbContext dbContext, IMapper mapper) : IHedgehogDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<IList<Models.Hedgehog>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.Hedgehog>> Query(Func<Models.Hedgehog, bool> predicate)
        {
            IEnumerable<Entities.Hedgehog> entities = await this.dbContext.Hedgehogs.Include(e => e.HomeTerritory)
                                                                                    .Include(e => e.CurrentTerritory)
#nullable disable
                                                                                    .ThenInclude(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
#nullable restore
                                                                                    .Include(e => e.Community)
                                                                                    .OrderBy(e => e.Name)
                                                                                    .ToListAsync();
            var models = this.mapper.Map<IList<Models.Hedgehog>>(entities);

            return models.Where(predicate).ToList();
        }
        public async Task<Models.Hedgehog?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.Hedgehog? entity = await this.dbContext.Hedgehogs.Where(e => e.Id == id)
                                                                      .Include(e => e.HomeTerritory)
                                                                      .Include(e => e.CurrentTerritory)
#nullable disable
                                                                      .ThenInclude(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
#nullable restore
                                                                      .Include(e => e.Community)
                                                                      .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }
            return this.mapper.Map<Models.Hedgehog>(entity);
        }

        public async Task<Guid> Insert(Models.Hedgehog model)
        {
            Entities.Hedgehog entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = model.Name,
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

        public async Task<IList<Hedgehog>> FindAllActiveForCommunity(Guid? communityId)
        {
            IEnumerable<Entities.Hedgehog> entities = await this.dbContext.Hedgehogs.Where(e => e.CommunityId == communityId && !e.DeletedDate.HasValue)
                                                                                    .Include(e => e.HomeTerritory)
                                                                                    .Include(e => e.CurrentTerritory)
#nullable disable
                                                                      .ThenInclude(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
#nullable restore
                                                                                    .Include(e => e.Community)
                                                                                    .OrderBy(e => e.Name)
                                                                                    .ToListAsync();
            return this.mapper.Map<IList<Models.Hedgehog>>(entities);
        }
    }
}
