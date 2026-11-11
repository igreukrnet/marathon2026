namespace Restaurant_Aqa_Framework.Api.Dtos.GuestSessions;

public sealed record CreateGuestSessionRequest(
    string GroupReservationId,
    string GuestName);
