using ConferenceRoomBooking.Api.Models;

namespace ConferenceRoomBooking.Api.Services;

public class PriceCalculator
{
    // Tariff zones in priority order: the first zone that contains the hour wins.
    // Peak goes first because it lies inside standard hours and must override them.
    private static readonly TariffZone[] TariffZones =
    {
        new(FromHour: 12, ToHour: 14, Multiplier: 1.15m), // peak: +15%
        new(FromHour: 6, ToHour: 9, Multiplier: 0.9m),    // morning: -10%
        new(FromHour: 9, ToHour: 18, Multiplier: 1.0m),   // standard
        new(FromHour: 18, ToHour: 23, Multiplier: 0.8m)   // evening: -20%
    };

    /// <summary>
    /// Calculates the total booking price: room price hour by hour
    /// (each hour uses its own tariff) plus a one-time fee for each selected service.
    /// </summary>
    public decimal CalculateTotal(decimal basePricePerHour, DateTime startTime, DateTime endTime, IEnumerable<Service> services)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be later than start time.");

        var roomPrice = 0m;

        // Walk through the booking one hour at a time
        for (var hour = startTime; hour < endTime; hour = hour.AddHours(1))
        {
            roomPrice += basePricePerHour * GetHourMultiplier(hour.Hour);
        }

        // Services are charged once per booking, not per hour
        var servicesPrice = services.Sum(s => s.Price);

        // Money is rounded to 2 decimals the usual way (0.005 -> 0.01)
        return Math.Round(roomPrice + servicesPrice, 2, MidpointRounding.AwayFromZero);
    }

    // Returns the price multiplier for one hour of the day (0–23)
    private static decimal GetHourMultiplier(int hour)
    {
        var zone = TariffZones.FirstOrDefault(z => z.Contains(hour))
            ?? throw new ArgumentOutOfRangeException(nameof(hour), "Booking is not allowed between 23:00 and 06:00");

        return zone.Multiplier;
    }

    private record TariffZone(int FromHour, int ToHour, decimal Multiplier)
    {
        public bool Contains(int hour) => hour >= FromHour && hour < ToHour;
    }
}