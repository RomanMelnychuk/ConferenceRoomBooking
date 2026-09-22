namespace ConferenceRoomBooking.Api.Exceptions;

// Resource does not exist -> 404
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}