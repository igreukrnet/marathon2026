using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GroupParticipantResponse(
    string? Id,
    string Type,
    string Name,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    bool IsSubmitted);
