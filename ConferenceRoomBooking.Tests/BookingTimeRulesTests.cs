using ConferenceRoomBooking.Api.Exceptions;
using ConferenceRoomBooking.Api.Services;

namespace ConferenceRoomBooking.Tests;

public class BookingTimeRulesTests
{
    // A fixed date far in the future, so the "no past bookings" rule never interferes
    private static readonly DateTime FutureDay = new(2030, 6, 10);

    [Fact]
    public void Validate_ValidSlot_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(10), FutureDay.AddHours(12)));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WholeWorkingDay_DoesNotThrow()
    {
        // 06:00-23:00 is the longest allowed booking
        var exception = Record.Exception(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(6), FutureDay.AddHours(23)));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_EndBeforeStart_Throws()
    {
        Assert.Throws<BadRequestException>(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(12), FutureDay.AddHours(10)));
    }

    [Fact]
    public void Validate_NotWholeHour_Throws()
    {
        Assert.Throws<BadRequestException>(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(10).AddMinutes(30), FutureDay.AddHours(12)));
    }

    [Fact]
    public void Validate_StartsBeforeOpening_Throws()
    {
        Assert.Throws<BadRequestException>(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(5), FutureDay.AddHours(7)));
    }

    [Fact]
    public void Validate_CrossesMidnight_Throws()
    {
        Assert.Throws<BadRequestException>(() =>
            BookingTimeRules.Validate(FutureDay.AddHours(22), FutureDay.AddHours(24)));
    }

    [Fact]
    public void Validate_InThePast_Throws()
    {
        var yesterday = DateTime.Today.AddDays(-1);

        Assert.Throws<BadRequestException>(() =>
            BookingTimeRules.Validate(yesterday.AddHours(10), yesterday.AddHours(12)));
    }
}