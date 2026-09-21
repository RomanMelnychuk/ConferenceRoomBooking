using ConferenceRoomBooking.Api.Exceptions;

namespace ConferenceRoomBooking.Api.Services;

// Business rules for a booking time slot, shared by search and booking
public static class BookingTimeRules
{
    public const int OpeningHour = 6;
    public const int ClosingHour = 23;

    public static void Validate(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new BadRequestException("End time must be later than start time.");

        if (!IsWholeHour(start) || !IsWholeHour(end))
            throw new BadRequestException("Bookings are made in whole hours, e.g. 10:00 or 14:00.");

        if (start.Date != end.Date)
            throw new BadRequestException("Booking must start and end on the same day.");

        if (start.Hour < OpeningHour || end.Hour > ClosingHour)
            throw new BadRequestException($"Rooms are available from {OpeningHour:00}:00 to {ClosingHour:00}:00.");

        if (start < DateTime.Now)
            throw new BadRequestException("Booking time must be in the future.");
    }

    private static bool IsWholeHour(DateTime time) =>
        time.Minute == 0 && time.Second == 0 && time.Millisecond == 0;
}