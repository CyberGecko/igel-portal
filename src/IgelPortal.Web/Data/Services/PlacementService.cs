using System.Data;
using IgelPortal.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace IgelPortal.Web.Data.Services;

public sealed record PlacementOffer(Guid Id, Guid TerritoryId, Guid? CommunityId, string TerritoryName, string City,
    Guid ServiceId, string ServiceName, string UserName, int Capacity, int Occupied, bool Paused)
{
    public int Free => Math.Max(0, Capacity - Occupied);
}

public sealed record PlacementAssignment(string? Email, string Territory, string Hedgehog, string Service);

public enum PlacementReply { Available, Declined, Withdraw }

public sealed class PlacementService(ApplicationDbContext db)
{
    public Task<List<PlacementOffer>> OffersAsync(Guid? groupId, Guid? serviceId = null, string? owner = null, bool freeOnly = true) =>
        OfferQuery(groupId, serviceId, owner, freeOnly)
            .OrderBy(offer => offer.Territory!.Name).ThenBy(offer => offer.TerritoryService!.Name)
            .Select(offer => new PlacementOffer(offer.Id, offer.TerritoryId, offer.Territory!.CommunityId,
                offer.Territory!.Name, offer.Territory!.City, offer.TerritoryServiceId, offer.TerritoryService!.Name,
                offer.Territory!.UserName!, offer.MaxCapacity, offer.Hedgehogs.Count(hedgehog => hedgehog.DeletedDate == null), offer.IsPaused))
            .ToListAsync();

    public Task<Dictionary<string, string>> ConfirmedRecipientsAsync(IEnumerable<PlacementOffer> offers)
    {
        var owners = offers.Select(offer => offer.UserName).Distinct().ToArray();
        return db.Users.AsNoTracking().Where(user => owners.Contains(user.UserName!) && user.EmailConfirmed && user.Email != null && user.Email != "")
            .ToDictionaryAsync(user => user.UserName!, user => user.Email!);
    }

    public async Task<bool> ReplyAsync(Guid requestId, string userName, PlacementReply reply, Guid? offerId)
    {
        if (!Enum.IsDefined(reply)) return false;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var request = await OpenRequests().SingleOrDefaultAsync(item => item.Id == requestId);
            if (request is null || !await IsMemberAsync(request.CommunityId, userName)) return false;

            var current = await db.ServiceRequestResponses.FindAsync(requestId, userName);
            if (reply == PlacementReply.Withdraw)
            {
                if (current is not null) db.ServiceRequestResponses.Remove(current);
            }
            else
            {
                var ownOffers = OfferQuery(request.CommunityId, request.ServiceId, userName, freeOnly: reply == PlacementReply.Available);
                if (reply == PlacementReply.Available
                    ? !offerId.HasValue || !await ownOffers.AnyAsync(offer => offer.Id == offerId)
                    : current is null && !await ownOffers.AnyAsync())
                {
                    return false;
                }
                if (current is null)
                {
                    current = new ServiceRequestResponse { ServiceRequestId = requestId, RespondingUserName = userName };
                    db.ServiceRequestResponses.Add(current);
                }
                current.IsAvailable = reply == PlacementReply.Available;
                current.OfferedTerritoryServiceId = current.IsAvailable ? offerId : null;
                current.ResponseDate = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception exception) when (IsConcurrentChange(exception))
        {
            return false;
        }
    }

    public async Task<PlacementAssignment?> AssignAsync(Guid requestId, Guid offerId, string manager)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var request = await OpenRequests().Include(item => item.Hedgehog).Include(item => item.Service)
                .Include(item => item.Responses).SingleOrDefaultAsync(item => item.Id == requestId);
            if (request is null || !await db.CommunityMembers.AnyAsync(member => member.CommunityId == request.CommunityId &&
                member.UserName == manager && (member.CommunityRoleName == CommunityRoles.OWNER ||
                    member.CommunityRoleName == CommunityRoles.MODERATOR)))
                return null;

            var offer = await OfferQuery(request.CommunityId, request.ServiceId, owner: null, freeOnly: true)
                .Include(item => item.Territory).SingleOrDefaultAsync(item => item.Id == offerId);
            if (offer is null || !request.Responses.Any(reply => reply.IsAvailable &&
                reply.RespondingUserName == offer.Territory!.UserName &&
                (reply.OfferedTerritoryServiceId == offer.Id || reply.OfferedTerritoryServiceId == null)))
                return null;

            var now = DateTime.UtcNow;
            request.Hedgehog.CurrentTerritoryId = offer.TerritoryId;
            request.Hedgehog.UsedServiceId = offer.Id;
            request.Hedgehog.ChangeDate = now;
            request.AssignedTerritoryServiceId = offer.Id;
            request.AssignedTerritoryName = offer.Territory!.Name;
            request.AssignedUserName = offer.Territory!.UserName;
            request.FulfillmentDate = now;
            request.ChangeDate = now;

            var email = await db.Users.Where(user => user.UserName == offer.Territory!.UserName && user.EmailConfirmed)
                .Select(user => user.Email).SingleOrDefaultAsync();
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new(email, offer.Territory!.Name, request.Hedgehog.Name, request.Service.Name);
        }
        catch (Exception exception) when (IsConcurrentChange(exception))
        {
            return null;
        }
    }

    public async Task<bool> CloseAsync(Guid requestId)
    {
        var now = DateTime.UtcNow;
        return await OpenRequests().Where(request => request.Id == requestId).ExecuteUpdateAsync(update => update
            .SetProperty(request => request.FulfillmentDate, now)
            .SetProperty(request => request.ChangeDate, now)) == 1;
    }

    public IQueryable<ServiceRequest> OpenRequests() =>
        db.ServiceRequests.Where(request => request.FulfillmentDate == null && request.DeletedDate == null &&
            request.Hedgehog.DeletedDate == null && request.Hedgehog.CommunityId == request.CommunityId &&
            request.Service.DeletedDate == null && request.Community.DeletedDate == null);

    private IQueryable<TerritoryAttachedService> OfferQuery(Guid? groupId, Guid? serviceId, string? owner, bool freeOnly)
    {
        var offers = db.TerritoryAttachedServices.Where(offer => offer.Territory != null && offer.Territory.DeletedDate == null &&
            offer.TerritoryService != null && offer.TerritoryService.DeletedDate == null && offer.Territory!.UserName != null);
        if (groupId.HasValue)
        {
            offers = offers.Where(offer => offer.Territory!.CommunityId == groupId &&
                db.CommunityMembers.Any(member => member.CommunityId == groupId && member.UserName == offer.Territory!.UserName &&
                    member.CommunityRoleName != CommunityRoles.APPLICANT && member.Community.DeletedDate == null));
        }
        if (serviceId.HasValue) offers = offers.Where(offer => offer.TerritoryServiceId == serviceId);
        if (owner is not null) offers = offers.Where(offer => offer.Territory!.UserName == owner);
        if (freeOnly) offers = offers.Where(offer => !offer.IsPaused &&
            offer.MaxCapacity > offer.Hedgehogs.Count(hedgehog => hedgehog.DeletedDate == null));
        return offers;
    }

    private Task<bool> IsMemberAsync(Guid communityId, string userName) =>
        db.CommunityMembers.AnyAsync(member => member.CommunityId == communityId && member.UserName == userName &&
            member.CommunityRoleName != CommunityRoles.APPLICANT && member.Community.DeletedDate == null);

    private static bool IsConcurrentChange(Exception exception) =>
        (exception is DbUpdateException ? exception.InnerException : exception) is MySqlException
        { ErrorCode: MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout or MySqlErrorCode.DuplicateKeyEntry };
}
