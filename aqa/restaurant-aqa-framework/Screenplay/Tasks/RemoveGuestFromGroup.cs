using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class RemoveGuestFromGroup(
    string guestName,
    string? groupReservationId = null,
    string? guestSessionId = null,
    string? bearerToken = null,
    bool throughUi = false,
    bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var targetGroupId = groupReservationId ?? actor.Recall<GuestInviteResponse>().GroupReservationId;

        if (throughUi && actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var groupOrderPage = new GroupOrderPage(page);
            await groupOrderPage.ConfirmRemoveGuestAsync(guestName);
            return;
        }

        string sessionId;
        if (!string.IsNullOrEmpty(guestSessionId))
        {
            sessionId = guestSessionId;
        }
        else if (!string.IsNullOrEmpty(guestName))
        {
            sessionId = actor.Recall<GuestSessionInfo>(guestName).SessionId;
        }
        else
        {
            sessionId = actor.Recall<GuestSessionInfo>().SessionId;
        }

        var token = bearerToken ?? (actor.TryRecall<CustomerSession>(out var customer) ? customer.IdToken : null);
        var groupApi = new GroupReservationsApi(actor.UsingTheApi());
        var rawResponse = await groupApi.DeleteGuestSessionAsync(targetGroupId, sessionId, token);

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok && mustSucceed)
        {
            throw new InvalidOperationException(
                $"Removing guest '{guestName}' failed with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
        }
    }
}
