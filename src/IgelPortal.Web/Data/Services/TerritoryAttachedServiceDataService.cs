using IgelPortal.Web.Data.Models.Mapping;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Services
{
    public class TerritoryAttachedServiceDataService(ApplicationDbContext dbContext) : ITerritoryAttachedServiceDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        public async Task<IList<Models.TerritoryAttachedService>> FindAll()
        {
            return await this.Query(c => true);
        }
        public async Task<IList<Models.TerritoryAttachedService>> Query(Func<Models.TerritoryAttachedService, bool> predicate)
        {
            IEnumerable<Entities.TerritoryAttachedService> entities = await this.dbContext.TerritoryAttachedServices.Include(e => e.Territory)
                                                                                                                    .Include(e => e.TerritoryService)
                                                                                                                    .ToListAsync();


            var models = entities.Select(TerritoryAttachedServiceMapping.Map).ToList();

            return models.Where(predicate).ToList();
        }
        public async Task<Models.TerritoryAttachedService?> FindOne(Guid id)
        {
            Entities.TerritoryAttachedService? entity = await this.dbContext.TerritoryAttachedServices
                                                                            .Include(e => e.Territory)
                                                                            .Include(e => e.TerritoryService)
                                                                            .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }

            return TerritoryAttachedServiceMapping.Map(entity);
        }

        public async Task<Int32> Insert(Models.TerritoryAttachedService model)
        {
            Entities.TerritoryAttachedService entity = new()
            {
                Id = Guid.NewGuid(),
                TerritoryServiceId = model.TerritoryServiceId,
                IsPaused = model.IsPaused,
                MaxCapacity = model.MaxCapacity
            };
            this.dbContext.Add(entity);
            return await this.dbContext.SaveChangesAsync();
        }
        public async Task<Int32> Update(Models.TerritoryAttachedService model)
        {
            try
            {
                Entities.TerritoryAttachedService? entity = await this.dbContext.TerritoryAttachedServices.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }

                entity.TerritoryServiceId = model.TerritoryServiceId;
                entity.MaxCapacity = model.MaxCapacity;
                entity.IsPaused = model.IsPaused;

                return await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<Int32> Delete(Guid id)
        {
            try
            {
                Entities.TerritoryAttachedService? entity = await this.dbContext.TerritoryAttachedServices.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return 0;
                }
                this.dbContext.Remove(entity);

                return await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return 0;
            }
        }
    }
}
