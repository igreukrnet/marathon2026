namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GroupStatusResponse(
    string Status,
    int TotalGuests,
    int SubmittedGuests);
