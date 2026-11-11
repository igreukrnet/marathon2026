using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

public sealed class TheConsolidatedOrder(string? reservationId = null) : IQuestion<GetOrderResponse?>
{
    public async Task<GetOrderResponse?> AnsweredByAsync(Actor actor)
    {
        var targetReservationId = reservationId ?? actor.Recall<ReservationResponse>().Id;
        var waiter = await actor.AsksForAsync(new TheWaiterSession());
        var ordersApi = new OrdersApi(actor.UsingTheApi());

        var response = await ordersApi.GetOrderAsync(targetReservationId, waiter.IdToken);
        if (!response.Ok)
            return null;

        return await response.ReadAsAsync<GetOrderResponse>();
    }
}
