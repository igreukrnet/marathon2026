using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class JoinGroupAsGuest(string guestName, string? groupReservationId = null, bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var targetGroupId = groupReservationId ?? actor.Recall<GuestInviteResponse>().GroupReservationId;
        var guestApi = new GuestSessionsApi(actor.UsingTheApi());
        var rawResponse = await guestApi.CreateGuestSessionAsync(new CreateGuestSessionRequest(targetGroupId, guestName));

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok)
        {
            if (mustSucceed)
                throw new InvalidOperationException(
                    $"Guest '{guestName}' failed to join group with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
            return;
        }

        var sessionResponse = await rawResponse.ReadAsAsync<CreateGuestSessionResponse>();
        var sessionInfo = new GuestSessionInfo(guestName, sessionResponse.SessionId, sessionResponse.SessionToken);

        actor.Remember(sessionInfo);
        actor.Remember(guestName, sessionInfo);
    }
}
