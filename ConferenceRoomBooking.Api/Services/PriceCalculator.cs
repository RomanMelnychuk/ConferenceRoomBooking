using ConferenceRoomBooking.Api.Models;

namespace ConferenceRoomBooking.Api.Services;

public class PriceCalculator
{
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

        return roomPrice + servicesPrice;
    }

    // Returns the price multiplier for one hour of the day (0–23)
    private decimal GetHourMultiplier(int hour)
    {
        if (hour >= 6 && hour < 9) return 0.9m;    // morning: -10%
        if (hour >= 12 && hour < 14) return 1.15m; // peak: +15%
        if (hour >= 9 && hour < 18) return 1.0m;   // standard
        if (hour >= 18 && hour < 23) return 0.8m;  // evening: -20%

        throw new ArgumentOutOfRangeException(nameof(hour), "Booking is not allowed between 23:00 and 06:00");
    }
}