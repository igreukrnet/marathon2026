namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GuestInviteResponse(
    string GroupReservationId,
    string RestaurantName,
    string LocationAddress,
    DateTime StartTime,
    DateTime? EndTime = null,
    int? MaxGuestCount = null);
