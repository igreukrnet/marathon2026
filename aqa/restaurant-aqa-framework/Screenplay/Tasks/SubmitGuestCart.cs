using Restaurant_Aqa_Framework.Api;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SubmitGuestCart(string? guestName = null, bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var session = guestName is null
            ? actor.Recall<GuestSessionInfo>()
            : actor.Recall<GuestSessionInfo>(guestName);

        var guestApi = new GuestSessionsApi(actor.UsingTheApi());
        var rawResponse = await guestApi.SubmitGuestCartAsync(session.SessionId, session.SessionToken);

        await actor.TheLastResponse().CaptureAsync(rawResponse);
        if (!rawResponse.Ok && mustSucceed)
            throw new InvalidOperationException(
                $"Submitting cart for guest '{session.GuestName}' failed with {rawResponse.Status}: {await rawResponse.ReadErrorMessageAsync()}");
    }
}
