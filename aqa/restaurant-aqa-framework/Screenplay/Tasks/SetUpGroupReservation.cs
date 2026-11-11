using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

public sealed class SetUpGroupReservation(int guestCount = 4, string? name = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        if (!actor.TryRecall<CustomerSession>(out var customer))
        {
            await actor.AttemptsToAsync(new RegisterAndSignInCustomer());
            customer = actor.Recall<CustomerSession>();
        }

        if (!actor.TryRecall<LocationResponse>(out var location))
        {
            await actor.AttemptsToAsync(new SetUpLocation());
            location = actor.Recall<LocationResponse>();
        }

        if (Configuration.Settings.Current.WaiterUsername is { Length: > 0 } waiterEmail)
        {
            var waitersApi = new WaitersApi(actor.UsingTheApi());
            await waitersApi.AssignWaiterToLocationAsync(location.Id, waiterEmail);
        }

        if (!actor.TryRecall<TableResponse>(out var table) || table.Capacity < guestCount)
        {
            await actor.AttemptsToAsync(new AddTable(guestCount));
            table = actor.Recall<TableResponse>();
        }

        var reservationsApi = new ReservationsApi(actor.UsingTheApi());
        var startTime = DateTime.UtcNow.AddDays(1).Date.AddHours(19);
        var request = SampleDataFactory.CreateReservationRequest(table.Id, startTime, guestCount);

        var createResponse = await reservationsApi.CreateReservationAsync(request, customer.IdToken);
        if (!createResponse.Ok)
            throw new InvalidOperationException(
                $"Creating group reservation failed with {createResponse.Status}: {await createResponse.ReadErrorMessageAsync()}");

        var reservation = await createResponse.ReadAsAsync<ReservationResponse>();

        var groupApi = new GroupReservationsApi(actor.UsingTheApi());
        var tokenResponse = await groupApi.GetInviteTokenAsync(reservation.Id, customer.IdToken);
        if (!tokenResponse.Ok)
            throw new InvalidOperationException(
                $"Fetching invite token failed with {tokenResponse.Status}: {await tokenResponse.ReadErrorMessageAsync()}");

        var inviteToken = await tokenResponse.ReadAsAsync<GetInviteTokenResponse>();
        var inviteDetailsResponse = await groupApi.GetGroupInviteAsync(inviteToken.Token);
        if (!inviteDetailsResponse.Ok)
            throw new InvalidOperationException(
                $"Fetching group invite failed with {inviteDetailsResponse.Status}: {await inviteDetailsResponse.ReadErrorMessageAsync()}");

        var groupInvite = await inviteDetailsResponse.ReadAsAsync<GuestInviteResponse>();

        // Create an initial order for the reservation so that group submit can populate it
        var dish = actor.Recall<DishResponse>();
        var ordersApi = new OrdersApi(actor.UsingTheApi());
        var orderResponse = await ordersApi.CreateOrderAsync(
            new Restaurant_Aqa_Framework.Api.Dtos.Orders.CreateOrderRequest(
                reservation.Id,
                [new Restaurant_Aqa_Framework.Api.Dtos.Orders.OrderItemRequest(dish.Id, 1)]),
            customer.IdToken);
        if (!orderResponse.Ok)
            throw new InvalidOperationException(
                $"Creating initial order failed with {orderResponse.Status}: {await orderResponse.ReadErrorMessageAsync()}");

        actor.Recall<CleanupScope>().TrackDelete(
            $"order:{reservation.Id}",
            () => ordersApi.DeleteOrderAsync(reservation.Id, customer.IdToken));

        if (name is null)
        {
            actor.Remember(reservation);
            actor.Remember(inviteToken);
            actor.Remember(groupInvite);
        }
        else
        {
            actor.Remember(name, reservation);
            actor.Remember(name, inviteToken);
            actor.Remember(name, groupInvite);
        }

        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservationsApi.CancelReservationAsync(reservation.Id, customer.IdToken));
    }
}
