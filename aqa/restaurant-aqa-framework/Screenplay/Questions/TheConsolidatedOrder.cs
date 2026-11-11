using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheConsolidatedOrder(string? reservationId = null) : IQuestion<GetOrderResponse?>
{
    public async Task<GetOrderResponse?> AnsweredByAsync(Actor actor)
    {
        var targetReservationId = reservationId ?? actor.Recall<ReservationResponse>().Id;
        var ordersApi = new OrdersApi(actor.UsingTheApi());

        string? token = null;
        if (actor.TryRecall<CustomerSession>(out var customer))
            token = customer.IdToken;
        else if (actor.TryRecall<WaiterSession>(out var waiter))
            token = waiter.IdToken;

        var response = await ordersApi.GetOrderAsync(targetReservationId, token);
        if (!response.Ok && actor.TryRecall<CustomerSession>(out var cust))
        {
            response = await ordersApi.GetOrderAsync(targetReservationId, cust.IdToken);
        }

        if (!response.Ok)
        {
            var body = await response.TextAsync();
            throw new InvalidOperationException(
                $"GetOrderAsync for reservation '{targetReservationId}' failed with {response.Status}: {body}");
        }

        return await response.ReadAsAsync<GetOrderResponse>();
    }
}
