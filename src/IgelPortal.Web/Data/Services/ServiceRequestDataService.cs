using AutoMapper;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace IgelPortal.Web.Data.Services
{
    public class ServiceRequestDataService(ApplicationDbContext dbContext, IMapper mapper) : IServiceRequestDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));


        public async Task<IList<Models.ServiceRequest>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.ServiceRequest>> Query(Func<Models.ServiceRequest, Boolean> predicate)
        {
            IEnumerable<Entities.ServiceRequest> entities = await this.dbContext.ServiceRequests.Include(e => e.Service)
                                                                                                .Include(e => e.Hedgehog)
                                                                                                .Include(e => e.Responses)
                                                                                                .OrderByDescending(e => e.CreationDate)
                                                                                                .ToListAsync();
            var models = this.mapper.Map<IList<Models.ServiceRequest>>(entities);

            return models.Where(predicate).ToList();
        }

        public async Task<Models.ServiceRequest?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.Where(e => e.Id == id)
                                                                                  .Include(e => e.Service)
                                                                                  .Include(e => e.Hedgehog)
                                                                                  .Include(e => e.Responses)
                                                                                  .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }
            return this.mapper.Map<Models.ServiceRequest>(entity);
        }
        public async Task<Models.ServiceRequest?> FindOneByToken(Guid? requestToken)
        {
            Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.Include(e => e.Service)
                                                                                  .Include(e => e.Hedgehog)
                                                                                  .Include(e => e.Responses)
                                                                                  .FirstOrDefaultAsync(x => x.RequestToken == requestToken);
            if (entity == null)
            {
                return null;
            }
            return this.mapper.Map<Models.ServiceRequest>(entity);
        }

        public async Task<Guid> Insert(Models.ServiceRequest model)
        {
            Entities.ServiceRequest entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                CommunityId = model.CommunityId,
                ServiceId = model.ServiceId,
                HedgehogId = model.HedgehogId,
                RequestToken = model.RequestToken,
                FulfillmentDate = model.FulfillmentDate,
            };
            entity.ChangeDate = entity.CreationDate;
            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<Int32> Update(Models.ServiceRequest model)
        {
            try
            {
                Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }
                entity.ChangeDate = DateTime.UtcNow;
                entity.CommunityId = model.CommunityId;
                entity.ServiceId = model.ServiceId;
                entity.HedgehogId = model.HedgehogId;
                entity.RequestToken = model.RequestToken;
                entity.FulfillmentDate = model.FulfillmentDate;

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
                Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id);
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

        public async Task AddResponseToRequest(Guid id, IIdentity? identity)
        {
            if (String.IsNullOrWhiteSpace(identity?.Name))
            {
                return;
            }
            Entities.ServiceRequestResponse entity = new()
            {
                RespondingUserName = identity.Name,
                ResponseDate = DateTime.UtcNow,
                ServiceRequestId = id
            };
            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
        }

        public async Task<IList<Models.ServiceRequest>> FindAllActiveForCommunityAndHedgehog(Guid? communityId, Guid? hedgehogId)
        {
            IEnumerable<Entities.ServiceRequest> entities = await this.dbContext.ServiceRequests.Where(e => !e.DeletedDate.HasValue && e.HedgehogId == hedgehogId && e.CommunityId == communityId)
                                                                                                .Include(e => e.Service)
                                                                                                .Include(e => e.Hedgehog)
                                                                                                .Include(e => e.Responses)
                                                                                                .OrderByDescending(e => e.CreationDate)
                                                                                                .ToListAsync();
            return this.mapper.Map<IList<Models.ServiceRequest>>(entities);
        }
    }
}
