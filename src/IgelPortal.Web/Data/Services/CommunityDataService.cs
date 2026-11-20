using AutoMapper;
using IgelPortal.Web.Interfaces;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.Spreadsheet;
using System.Data;
using System.Security.Principal;

namespace IgelPortal.Web.Data.Services
{
    public class CommunityDataService(ApplicationDbContext dbContext, IMapper mapper) : ICommunityDataService
    {
        private readonly ApplicationDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IMapper mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<IList<Models.Community>> FindAll()
        {
            return await this.Query(c => c.IsDeleted == false);
        }
        public async Task<IList<Models.Community>> Query(Func<Models.Community, bool> predicate)
        {
            IEnumerable<Entities.Community> entities = await this.dbContext.Communities.Include(e => e.Hedgehogs)
                                                                                       .Include(e => e.Territories)
                                                                                            .ThenInclude(e => e.AttachedServices)
                                                                                            .ThenInclude(e => e.TerritoryService)
                                                                                       .OrderBy(e => e.Name)
                                                                                       .ToListAsync();


            var models = this.mapper.Map<IList<Models.Community>>(entities);

            return models.Where(predicate).ToList();
        }
        public async Task<Models.Community?> FindOne(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            Entities.Community? entity = await this.dbContext.Communities.Where(e => e.Id == id)
                                                                         .Include(e => e.Hedgehogs)
                                                                         .Include(e => e.Territories).ThenInclude(e => e.AttachedServices).ThenInclude(e => e.TerritoryService)
                                                                         .Include(e => e.CommunityMembers)
                                                                         .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return null;
            }
            Models.Community model = this.mapper.Map<Models.Community>(entity);

            return this.mapper.Map<Models.Community>(entity);
        }

        public async Task<Guid> Insert(Models.Community model)
        {
            Entities.Community entity = new()
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = model.Name,
                ContactMail = model.ContactMail,
                ContactPhone = model.ContactPhone,
                ContactName = model.ContactName,
                Description = model.Description,
                UserName = model.UserName
            };
            entity.ChangeDate = entity.CreationDate;
            this.dbContext.Add(entity);
            await this.dbContext.SaveChangesAsync();
            return entity.Id;
        }
        public async Task<Int32> Update(Models.Community model)
        {
            try
            {
                Entities.Community? entity = await this.dbContext.Communities.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null)
                {
                    return 0;
                }
                entity.ChangeDate = DateTime.UtcNow;
                entity.Name = model.Name;
                entity.ContactMail = model.ContactMail;
                entity.ContactPhone = model.ContactPhone;
                entity.ContactName = model.ContactName;
                entity.Description = model.Description;
                entity.UserName = model.UserName;

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
                Entities.Community? entity = await this.dbContext.Communities.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return 0;
                }
                entity.DeletedDate = DateTime.UtcNow;
                await this.dbContext.Territories.Where(e => !e.DeletedDate.HasValue && e.CommunityId == id).ForEachAsync(e => { e.CommunityId = null; });
                await this.dbContext.Hedgehogs.Where(e => !e.DeletedDate.HasValue && e.CommunityId == id).ForEachAsync(e => { e.CommunityId = null; });
                await this.dbContext.CommunityMembers.Where(e => e.CommunityId == id && e.CommunityRoleName != CommunityRoles.OWNER).ForEachAsync(e => { this.dbContext.Remove(e); });

                return await this.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return 0;
            }
        }

        public async Task<Boolean> HasUserCommunityMemberRole(Guid? communityId, IIdentity? user, String? communityRole)
        {
            if (!communityId.HasValue || String.IsNullOrWhiteSpace(user?.Name) || String.IsNullOrWhiteSpace(communityRole))
            {
                return false;
            }
            return await this.dbContext.CommunityMembers.AnyAsync(e => e.CommunityId == communityId && e.UserName == user.Name && e.CommunityRoleName == communityRole);
        }

        public async Task<IList<Models.Community>> FindAllForUserWithRole(IIdentity? user, String? communityRole)
        {
            if (String.IsNullOrWhiteSpace(user?.Name) || String.IsNullOrWhiteSpace(communityRole))
            {
                return [];
            }
            var asignedCommunities = await this.dbContext.CommunityMembers.Where(e => e.UserName == user.Name && e.CommunityRoleName == communityRole)
                                                                    .Include(e => e.Community)
                                                                    .Select(c => c.Community).ToListAsync();
            return [.. asignedCommunities.Select(this.mapper.Map<Models.Community>).Where(c => !c.IsDeleted).OrderBy(c => c.Name)];
        }

        public async Task<IList<Models.CommunityMember>> GetCommunityMembers(Guid? communityId, Boolean includeApplicant)
        {
            IQueryable<Entities.CommunityMember> entities = includeApplicant ? this.dbContext.CommunityMembers.Where(e => e.CommunityId == communityId) :
                                                                               this.dbContext.CommunityMembers.Where(e => e.CommunityId == communityId && e.CommunityRoleName != CommunityRoles.APPLICANT);
            entities = entities.OrderBy(e => e.UserName);
            return (await entities.ToListAsync()).Select(this.mapper.Map<Models.CommunityMember>).ToList();
        }

        public async Task AcceptCommunityApplication(Guid? communityId, String userName)
        {
            var applicationEntity = await this.dbContext.CommunityMembers.FirstOrDefaultAsync(e => e.UserName == userName && e.CommunityId == communityId && e.CommunityRoleName == CommunityRoles.APPLICANT);
            if (applicationEntity == null)
            {
                return;
            }
            Entities.CommunityMember memberEntity = new()
            {
                CommunityId = applicationEntity.CommunityId,
                UserName = applicationEntity.UserName,
                CommunityRoleName = CommunityRoles.MEMBER
            };
            this.dbContext.CommunityMembers.Add(memberEntity);
            this.dbContext.CommunityMembers.Remove(applicationEntity);

            await this.dbContext.SaveChangesAsync();
        }

        public async Task RejectCommunityApplication(Guid? communityId, String userName)
        {
            var entity = await this.dbContext.CommunityMembers.FirstOrDefaultAsync(e => e.UserName == userName && e.CommunityId == communityId && e.CommunityRoleName == CommunityRoles.APPLICANT);
            if (entity == null)
            {
                return;
            }
            this.dbContext.CommunityMembers.Remove(entity);
            await this.dbContext.SaveChangesAsync();
        }

        public async Task PromoteUserToCommunityModerator(Guid? communityId, String userName)
        {
            if (!communityId.HasValue)
            {
                return;
            }

            await this.dbContext.CommunityMembers.AddAsync(new Entities.CommunityMember()
            {
                CommunityId = communityId.Value,
                UserName = userName,
                CommunityRoleName = CommunityRoles.MODERATOR
            });
        }

        public async Task DemoteUserFromCommunityModerator(Guid? communityId, String userName)
        {
            var entity = await this.dbContext.CommunityMembers.FirstOrDefaultAsync(e => e.UserName == userName && e.CommunityId == communityId && e.CommunityRoleName == CommunityRoles.MODERATOR);
            if (entity == null)
            {
                return;
            }
            this.dbContext.CommunityMembers.Remove(entity);
            await this.dbContext.SaveChangesAsync();
        }

        public async Task RemoveUserFromCommunity(Guid? communityId, String userName)
        {
            var communityMembers = await this.dbContext.CommunityMembers.Where(e => e.UserName == userName && e.CommunityId == communityId).ToListAsync();
            foreach (var communityMember in communityMembers)
            {
                this.dbContext.CommunityMembers.Remove(communityMember);
            }

            var userTerritories = await this.dbContext.Territories.Where(e => e.UserName == userName && e.CommunityId == communityId).ToListAsync();
            foreach (var userTerritory in userTerritories)
            {
                userTerritory.CommunityId = null;
            }

            var userHedgehogs = await this.dbContext.Hedgehogs.Where(e => e.AttachedUser == userName && e.CommunityId == communityId).ToListAsync();
            foreach (var userHedgehog in userHedgehogs)
            {
                userHedgehog.CommunityId = null;
            }

            await this.dbContext.SaveChangesAsync();
        }

        public async Task AddUserAsApplicant(Guid? communityId, IIdentity? user)
        {
            await this.AddUserAsRole(communityId, user, CommunityRoles.APPLICANT);
        }

        public async Task AddUserAsOwner(Guid? communityId, String userName)
        {
            await this.AddUserAsRole(communityId, userName, CommunityRoles.OWNER);
        }
        public async Task AddUserAsOwner(Guid? communityId, IIdentity? user)
        {
            await this.AddUserAsRole(communityId, user, CommunityRoles.OWNER);
        }

        public async Task AddUserAsMember(Guid? communityId, IIdentity? user)
        {
            await this.AddUserAsRole(communityId, user, CommunityRoles.MEMBER);
        }

        public async Task AddUserAsModerator(Guid? communityId, IIdentity? user)
        {
            await this.AddUserAsRole(communityId, user, CommunityRoles.MODERATOR);
        }

        private async Task AddUserAsRole(Guid? communityId, IIdentity? user, String communityRole)
        {
            await this.AddUserAsRole(communityId, user?.Name, communityRole);
        }
        private async Task AddUserAsRole(Guid? communityId, String? userName, String communityRole)
        {
            if (String.IsNullOrWhiteSpace(userName) || !communityId.HasValue)
            {
                return;
            }

            Entities.CommunityMember applicant = new()
            {
                CommunityId = communityId.Value,
                CommunityRoleName = communityRole,
                UserName = userName
            };

            await this.dbContext.CommunityMembers.AddAsync(applicant);

            await this.dbContext.SaveChangesAsync();
        }
    }
}
