using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class ViewSessionsForGroup(
    string? groupReservationId = null,
    string? bearerToken = null,
    bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var targetGroupId = groupReservationId ?? actor.Recall<GuestInviteResponse>().GroupReservationId;
        var token = bearerToken ?? (actor.TryRecall<CustomerSession>(out var customer) ? customer.IdToken : null);

        var groupApi = new GroupReservationsApi(actor.UsingTheApi());
        var rawResponse = await groupApi.GetAllSessionsAsync(targetGroupId, token);

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok)
        {
            if (mustSucceed)
                throw new InvalidOperationException(
                    $"Viewing group sessions failed with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
            return;
        }

        var sessionsResponse = await rawResponse.ReadAsAsync<GetAllSessionsResponse>();
        actor.Remember(sessionsResponse);
    }
}
