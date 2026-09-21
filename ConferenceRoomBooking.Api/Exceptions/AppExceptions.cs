namespace ConferenceRoomBooking.Api.Exceptions;

// Resource does not exist -> 404
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Request is invalid from a business point of view -> 400
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

// Request conflicts with the current state of data -> 409
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}