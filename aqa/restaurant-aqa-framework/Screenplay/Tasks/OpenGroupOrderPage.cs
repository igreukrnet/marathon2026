using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class OpenGroupOrderPage(string? reservationId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var id = reservationId ?? actor.Recall<ReservationResponse>().Id;
        var page = actor.LookingAtThePage();
        var groupOrderPage = new GroupOrderPage(page);

        await groupOrderPage.NavigateAsync(id);

        // Wait for page to finish loading: either title or error container appears
        await page.Locator("h1.page-title, .state-container.error-state").First.WaitForAsync();
    }
}
