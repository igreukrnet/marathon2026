using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheGroupGrandTotal : IQuestion<decimal>
{
    public async Task<decimal> AnsweredByAsync(Actor actor)
    {
        if (actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var groupOrderPage = new GroupOrderPage(page);
            var text = await groupOrderPage.GrandTotalPrice.InnerTextAsync();
            var cleanText = text.Replace("$", "", StringComparison.Ordinal).Trim();
            return decimal.TryParse(
                cleanText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var total) ? total : 0m;
        }

        if (actor.TryRecall<GetAllSessionsResponse>(out var sessions))
        {
            return sessions.Total;
        }

        return 0m;
    }
}
