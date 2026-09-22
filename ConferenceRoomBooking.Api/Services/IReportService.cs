using ConferenceRoomBooking.Api.Dtos;

namespace ConferenceRoomBooking.Api.Services;

public interface IReportService
{
    /// <summary>Bookings, booked hours, revenue and utilization for each room in the period.</summary>
    /// <exception cref="Exceptions.BadRequestException">Period is invalid.</exception>
    Task<List<RoomReportItem>> GetRoomReportAsync(ReportPeriodQuery query);

    /// <summary>How often each service was ordered in the period and how much it earned.</summary>
    /// <exception cref="Exceptions.BadRequestException">Period is invalid.</exception>
    Task<List<ServiceReportItem>> GetServiceReportAsync(ReportPeriodQuery query);

    /// <summary>How many booked hours fall into each tariff zone, to see whether discounts and the peak surcharge work.</summary>
    /// <exception cref="Exceptions.BadRequestException">Period is invalid.</exception>
    Task<List<TariffZoneReportItem>> GetTariffZoneReportAsync(ReportPeriodQuery query);
}