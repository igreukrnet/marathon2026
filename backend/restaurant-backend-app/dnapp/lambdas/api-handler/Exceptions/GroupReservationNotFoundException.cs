namespace APIHandler.Exceptions;

public class GroupReservationNotFoundException(string? message = null) 
    : ValidationException(message ?? "Group reservation was not found");
