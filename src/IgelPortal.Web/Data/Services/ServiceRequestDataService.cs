using MySqlConnector;
using IgelPortal.Web.Data.Models.Mapping;
using IgelPortal.Web.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace IgelPortal.Web.Data.Services
{
    public class ServiceRequestDataService(ApplicationDbContext dbContext) : IServiceRequestDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));


        public async Task<IList<Models.ServiceRequest>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.ServiceRequest>> Query(Func<Models.ServiceRequest, Boolean> predicate)
        {
            IEnumerable<Entities.ServiceRequest> entities = await this.dbContext.ServiceRequests.AsNoTracking().Include(e => e.Service)
                                                                                                .Include(e => e.Hedgehog)
                                                                                                .Include(e => e.Responses)
                                                                                                .OrderByDescending(e => e.CreationDate)
                                                                                                .ToListAsync();
            var models = await MapRequestsAsync(entities);

            return models.Where(predicate).ToList();
        }

        public async Task<Models.ServiceRequest?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.AsNoTracking().Where(e => e.Id == id)
                                                                                  .Include(e => e.Service)
                                                                                  .Include(e => e.Hedgehog)
                                                                                  .Include(e => e.Responses)
                                                                                  .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }
            return (await MapRequestsAsync([entity]))[0];
        }
        public async Task<Models.ServiceRequest?> FindOneByToken(Guid? requestToken)
        {
            Entities.ServiceRequest? entity = await this.dbContext.ServiceRequests.AsNoTracking().Include(e => e.Service)
                                                                                  .Include(e => e.Hedgehog)
                                                                                  .Include(e => e.Responses)
                                                                                  .FirstOrDefaultAsync(x => x.RequestToken == requestToken);
            if (entity == null)
            {
                return null;
            }
            return (await MapRequestsAsync([entity]))[0];
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
            if (await dbContext.ServiceRequestResponses.AnyAsync(response =>
                response.ServiceRequestId == id && response.RespondingUserName == identity.Name))
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
            try
            {
                await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                this.dbContext.Entry(entity).State = EntityState.Detached;
            }
        }

        public async Task<IList<Models.ServiceRequest>> FindAllActiveForCommunityAndHedgehog(Guid? communityId, Guid? hedgehogId)
        {
            IEnumerable<Entities.ServiceRequest> entities = await this.dbContext.ServiceRequests.AsNoTracking().Where(e => !e.DeletedDate.HasValue && e.HedgehogId == hedgehogId && e.CommunityId == communityId)
                                                                                                .Include(e => e.Service)
                                                                                                .Include(e => e.Hedgehog)
                                                                                                .Include(e => e.Responses)
                                                                                                .OrderByDescending(e => e.CreationDate)
                                                                                                .ToListAsync();
            return await MapRequestsAsync(entities);
        }
        private async Task<List<Models.ServiceRequest>> MapRequestsAsync(IEnumerable<Entities.ServiceRequest> entities)
        {
            var requests = entities.Select(ServiceRequestMapping.Map).ToList();
            var responses = requests.SelectMany(request => request.Responses).ToList();
            var names = await UserDisplayNames.LoadAsync(dbContext, responses.Select(response => response.RespondingUserName));
            foreach (var response in responses)
            {
                response.RespondingUserDisplayName = names.GetValueOrDefault(response.RespondingUserName, response.RespondingUserName);
            }
            return requests;
        }
    }
}
