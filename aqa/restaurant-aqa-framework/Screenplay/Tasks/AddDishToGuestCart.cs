using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class AddDishToGuestCart(string? guestName = null, int quantity = 1, string? dishName = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var session = guestName is null
            ? actor.Recall<GuestSessionInfo>()
            : actor.Recall<GuestSessionInfo>(guestName);

        DishResponse dish;
        if (!string.IsNullOrWhiteSpace(dishName))
        {
            dish = actor.Recall<DishResponse>(dishName);
        }
        else if (actor.TryRecall<DishResponse>(out var currentDish))
        {
            dish = currentDish;
        }
        else
        {
            await actor.AttemptsToAsync(new AddDish());
            dish = actor.Recall<DishResponse>();
        }

        var guestApi = new GuestSessionsApi(actor.UsingTheApi());
        var rawResponse = await guestApi.AddDishAsync(
            session.SessionId,
            new AddDishToGuestSessionRequest(dish.Id, quantity),
            session.SessionToken);

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok)
            throw new InvalidOperationException(
                $"Adding dish to guest '{session.GuestName}' failed with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
    }
}
