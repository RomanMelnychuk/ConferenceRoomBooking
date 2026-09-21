using ConferenceRoomBooking.Api.Models;
using ConferenceRoomBooking.Api.Services;

namespace ConferenceRoomBooking.Tests;

public class PriceCalculatorTests
{
    private const decimal BasePrice = 2000m;
    private static readonly DateTime Day = new(2026, 12, 1);
    private static readonly List<Service> NoServices = new();

    private readonly PriceCalculator _calculator = new();

    [Theory]
    [InlineData(6, 1800)]   // morning: 2000 * 0.9
    [InlineData(10, 2000)]  // standard
    [InlineData(12, 2300)]  // peak: 2000 * 1.15
    [InlineData(15, 2000)]  // standard after peak
    [InlineData(18, 1600)]  // evening: 2000 * 0.8
    [InlineData(22, 1600)]  // last evening hour
    public void CalculateTotal_OneHour_UsesTariffOfThatHour(int startHour, int expectedTotal)
    {
        var start = Day.AddHours(startHour);

        var total = _calculator.CalculateTotal(BasePrice, start, start.AddHours(1), NoServices);

        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void CalculateTotal_BookingCrossesTariffZones_PricesEachHourSeparately()
    {
        // 11:00 standard (2000) + 12:00 peak (2300)
        var total = _calculator.CalculateTotal(BasePrice, Day.AddHours(11), Day.AddHours(13), NoServices);

        Assert.Equal(4300m, total);
    }

    [Fact]
    public void CalculateTotal_WithServices_ChargesEachServiceOnce()
    {
        var services = new List<Service>
        {
            new() { Name = "Projector", Price = 500m },
            new() { Name = "Wi-Fi", Price = 300m }
        };

        // 3 standard hours (3 * 2000) + services once, not per hour (500 + 300)
        var total = _calculator.CalculateTotal(BasePrice, Day.AddHours(9), Day.AddHours(12), services);

        Assert.Equal(6800m, total);
    }

    [Fact]
    public void CalculateTotal_FractionalResult_RoundsToTwoDecimals()
    {
        // 1234.55 * 1.15 = 1419.7325 -> 1419.73
        var total = _calculator.CalculateTotal(1234.55m, Day.AddHours(12), Day.AddHours(13), NoServices);

        Assert.Equal(1419.73m, total);
    }

    [Fact]
    public void CalculateTotal_HourOutsideWorkingTime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.CalculateTotal(BasePrice, Day.AddHours(23), Day.AddHours(24), NoServices));
    }

    [Fact]
    public void CalculateTotal_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _calculator.CalculateTotal(BasePrice, Day.AddHours(12), Day.AddHours(10), NoServices));
    }
}