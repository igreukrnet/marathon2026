using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Screenplay.Abilities;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed record ParticipantSummary(string Name, decimal Total, bool IsSubmitted, int ItemsCount);

public sealed class TheGroupParticipants : IQuestion<IReadOnlyList<ParticipantSummary>>
{
    public async Task<IReadOnlyList<ParticipantSummary>> AnsweredByAsync(Actor actor)
    {
        if (actor.Can<IBrowseTheWeb>())
        {
            var page = actor.LookingAtThePage();
            var groupOrderPage = new GroupOrderPage(page);
            var cardsCount = await groupOrderPage.ParticipantCards.CountAsync();
            var result = new List<ParticipantSummary>();

            for (var i = 0; i < cardsCount; i++)
            {
                var card = groupOrderPage.Participant(i);
                var title = await card.GetTitleAsync();
                var subtotalText = await card.GetSubtotalAsync();
                // clean subtotalText (e.g. "$123.45" -> 123.45)
                var cleanTotal = decimal.TryParse(
                    subtotalText.Replace("$", "", StringComparison.Ordinal).Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var val) ? val : 0m;

                var isSubmitted = await card.IsSubmittedAsync();
                var itemsCount = await card.Items.CountAsync();

                result.Add(new ParticipantSummary(title, cleanTotal, isSubmitted, itemsCount));
            }

            return result;
        }

        if (actor.TryRecall<GetAllSessionsResponse>(out var sessions))
        {
            return sessions.Participants
                .Select(p => new ParticipantSummary(p.Name, p.Total, p.IsSubmitted, p.Items.Count))
                .ToList();
        }

        return [];
    }
}
