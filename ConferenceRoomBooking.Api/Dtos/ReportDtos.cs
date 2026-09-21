using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConferenceRoomBooking.Api.Dtos;

public class ReportPeriodQuery
{
    /// <summary>First day of the period, e.g. 2026-10-01</summary>
    [BindRequired]
    public DateOnly From { get; set; }

    /// <summary>Last day of the period (inclusive), e.g. 2026-10-31</summary>
    [BindRequired]
    public DateOnly To { get; set; }
}

public class RoomReportItem
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int BookingsCount { get; set; }
    public int BookedHours { get; set; }

    /// <summary>Total revenue of the room's bookings, services included</summary>
    public decimal Revenue { get; set; }

    /// <summary>Booked hours divided by available working hours, in percent</summary>
    public decimal UtilizationPercent { get; set; }
}

public class ServiceReportItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TimesOrdered { get; set; }
    public decimal Revenue { get; set; }
}