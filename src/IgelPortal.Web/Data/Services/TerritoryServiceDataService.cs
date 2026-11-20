using AutoMapper;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IgelPortal.Web.Data.Services
{
    public class TerritoryServiceDataService(ApplicationDbContext dbContext, IMapper mapper) : ITerritoryServiceDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<IList<Models.TerritoryService>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.TerritoryService>> Query(Func<Models.TerritoryService, bool> predicate)
        {
            IEnumerable<Entities.TerritoryService> entities = await this.dbContext.TerritoryServices.OrderBy(e => e.Name).ToListAsync();

            var models = this.mapper.Map<IList<Models.TerritoryService>>(entities);

            return models.Where(predicate).ToList();
        }
        public async Task<Models.TerritoryService?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.TerritoryService? entity = await this.dbContext.TerritoryServices.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }

            return this.mapper.Map<Models.TerritoryService>(entity);
        }

        public async Task<Guid> Insert(Models.TerritoryService model)
        {
            Entities.TerritoryService entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = model.Name
            };
            entity.ChangeDate = entity.CreationDate;
            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
            return entity.Id;
        }
        public async Task<Int32> Update(Models.TerritoryService model)
        {
            try
            {
                Entities.TerritoryService? entity = await this.dbContext.TerritoryServices.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }

                entity.ChangeDate = DateTime.UtcNow;
                entity.Name = model.Name;

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
                Entities.TerritoryService? entity = await this.dbContext.TerritoryServices.FirstOrDefaultAsync(x => x.Id == id);
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
