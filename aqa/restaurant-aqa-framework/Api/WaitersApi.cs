using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Api;

public sealed record AddWaiterToLocationRequest(string LocationId, string Email);
public sealed record DeleteWaiterRequest(string Email);

public sealed class WaitersApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> AddWaiterAsync(string locationId, string email) =>
        PostAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new AddWaiterToLocationRequest(locationId, email)),
            Headers = Headers(),
        });

    public Task<IAPIResponse> DeleteWaiterAsync(string email) =>
        DeleteAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new DeleteWaiterRequest(email)),
            Headers = Headers(),
        });

    public async Task AssignWaiterToLocationAsync(string locationId, string email)
    {
        var response = await AddWaiterAsync(locationId, email);
        if (response.Status == 409)
        {
            await DeleteWaiterAsync(email);
            await AddWaiterAsync(locationId, email);
        }
    }
}
