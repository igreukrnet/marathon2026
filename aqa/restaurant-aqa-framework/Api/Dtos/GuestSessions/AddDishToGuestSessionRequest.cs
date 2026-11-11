namespace Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

public sealed record AddDishToGuestSessionRequest(
    string DishId,
    int Quantity);
