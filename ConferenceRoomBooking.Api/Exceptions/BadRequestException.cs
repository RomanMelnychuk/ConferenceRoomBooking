namespace ConferenceRoomBooking.Api.Exceptions;

// Request is invalid from a business point of view -> 400
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}