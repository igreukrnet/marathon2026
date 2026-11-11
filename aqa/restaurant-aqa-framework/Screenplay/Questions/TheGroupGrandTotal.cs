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
            var consolidatedTotal = decimal.TryParse(
                cleanText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var total) ? total : 0m;

            if (consolidatedTotal > 0m)
                return consolidatedTotal;

            // When carts are not yet submitted, consolidated total is $0.00; sum visible participant subtotals
            var participants = await actor.AsksForAsync(new TheGroupParticipants());
            return participants.Sum(p => p.Total);
        }

        if (actor.TryRecall<GetAllSessionsResponse>(out var sessions))
        {
            return sessions.Total;
        }

        return 0m;
    }
}
