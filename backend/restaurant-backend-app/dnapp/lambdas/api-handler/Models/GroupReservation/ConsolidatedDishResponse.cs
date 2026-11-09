using System.Text.Json.Serialization;

namespace APIHandler.Models.GroupReservation;

public record ConsolidatedDishResponse(
    [property: JsonPropertyName("dishId")] string DishId,
    [property: JsonPropertyName("quantity")] int Quantity
);
