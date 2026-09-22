namespace ConferenceRoomBooking.Api.Exceptions;

// Request conflicts with the current state of data -> 409
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}