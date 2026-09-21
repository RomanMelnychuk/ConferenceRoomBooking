using ConferenceRoomBooking.Api.Data;
using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Api.Services;

public class ReportService : IReportService
{
    private const int MaxPeriodDays = 366;
    private const int WorkingHoursPerDay = BookingTimeRules.ClosingHour - BookingTimeRules.OpeningHour;

    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoomReportItem>> GetRoomReportAsync(ReportPeriodQuery query)
    {
        var (from, to) = GetPeriodBounds(query);
        var daysInPeriod = query.To.DayNumber - query.From.DayNumber + 1;
        var availableHours = daysInPeriod * WorkingHoursPerDay;

        var bookings = await _context.Bookings
            .Where(b => b.StartTime >= from && b.StartTime < to)
            .AsNoTracking()
            .ToListAsync();

        var rooms = await _context.ConferenceRooms
            .AsNoTracking()
            .ToListAsync();

        return rooms
            .Select(room =>
            {
                var roomBookings = bookings.Where(b => b.RoomId == room.Id).ToList();
                var bookedHours = roomBookings.Sum(b => (int)(b.EndTime - b.StartTime).TotalHours);

                return new RoomReportItem
                {
                    RoomId = room.Id,
                    RoomName = room.Name,
                    BookingsCount = roomBookings.Count,
                    BookedHours = bookedHours,
                    Revenue = roomBookings.Sum(b => b.TotalPrice),
                    UtilizationPercent = Math.Round(100m * bookedHours / availableHours, 1)
                };
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    public async Task<List<ServiceReportItem>> GetServiceReportAsync(ReportPeriodQuery query)
    {
        var (from, to) = GetPeriodBounds(query);

        var stats = await _context.Services
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Price,
                TimesOrdered = s.Bookings.Count(b => b.StartTime >= from && b.StartTime < to)
            })
            .ToListAsync();

        return stats
            .Select(s => new ServiceReportItem
            {
                ServiceId = s.Id,
                ServiceName = s.Name,
                TimesOrdered = s.TimesOrdered,
                // Uses the current service price: service prices are not stored per booking
                Revenue = s.TimesOrdered * s.Price
            })
            .OrderByDescending(s => s.TimesOrdered)
            .ThenByDescending(s => s.Revenue)
            .ToList();
    }

    // Turns a date range into [from, to): "to" is the start of the day after the last day,
    // so the whole last day is included
    private static (DateTime From, DateTime To) GetPeriodBounds(ReportPeriodQuery query)
    {
        if (query.To < query.From)
            throw new BadRequestException("'To' date must not be earlier than 'From' date.");

        if (query.To.DayNumber - query.From.DayNumber >= MaxPeriodDays)
            throw new BadRequestException($"Report period cannot be longer than {MaxPeriodDays} days.");

        return (query.From.ToDateTime(TimeOnly.MinValue), query.To.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }
}