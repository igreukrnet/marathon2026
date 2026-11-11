namespace Restaurant_Aqa_Framework.Api.Dtos.GroupReservations;

public sealed record GetAllSessionsResponse(
    IReadOnlyList<GroupParticipantResponse> Participants,
    decimal Total);
