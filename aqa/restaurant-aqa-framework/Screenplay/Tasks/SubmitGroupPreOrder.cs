using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SubmitGroupPreOrder(
    string? groupReservationId = null,
    string? reservationId = null,
    string? bearerToken = null,
    bool throughUi = false,
    bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var targetReservationId = reservationId ?? actor.Recall<ReservationResponse>().Id;
        var targetGroupId = groupReservationId ?? actor.Recall<GuestInviteResponse>().GroupReservationId;

        if (throughUi && actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var groupOrderPage = new GroupOrderPage(page);
            await groupOrderPage.ConfirmSubmitAsync();
            return;
        }

        var token = bearerToken ?? (actor.TryRecall<CustomerSession>(out var customer) ? customer.IdToken : null);
        var groupApi = new GroupReservationsApi(actor.UsingTheApi());
        var rawResponse = await groupApi.SubmitGroupPreOrderAsync(
            targetGroupId,
            new SubmitGroupPreOrderRequest(targetReservationId),
            token);

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok && mustSucceed)
        {
            throw new InvalidOperationException(
                $"Submitting group pre-order failed with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
        }
    }
}
