using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheGroupStatus : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor)
    {
        if (actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var groupOrderPage = new GroupOrderPage(page);
            return (await groupOrderPage.StatusBadge.InnerTextAsync()).Trim();
        }

        var groupInvite = actor.Recall<GuestInviteResponse>();
        var token = actor.TryRecall<CustomerSession>(out var customer) ? customer.IdToken : null;
        var groupApi = new GroupReservationsApi(actor.UsingTheApi());
        var response = await groupApi.GetGroupStatusAsync(groupInvite.GroupReservationId, token);

        if (!response.Ok)
            return "Unknown";

        var statusDto = await response.ReadAsAsync<GroupStatusResponse>();
        return statusDto.Status;
    }
}
