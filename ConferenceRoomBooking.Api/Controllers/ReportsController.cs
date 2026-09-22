using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Security;
using ConferenceRoomBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[AdminApiKey]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Bookings, booked hours, revenue and utilization for each room, sorted by revenue. Requires the admin API key.</summary>
    [HttpGet("rooms")]
    [ProducesResponseType(typeof(List<RoomReportItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RoomReportItem>>> GetRoomReport([FromQuery] ReportPeriodQuery query)
    {
        return Ok(await _reportService.GetRoomReportAsync(query));
    }

    /// <summary>How often each service was ordered and how much it earned, most popular first. Requires the admin API key.</summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(List<ServiceReportItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ServiceReportItem>>> GetServiceReport([FromQuery] ReportPeriodQuery query)
    {
        return Ok(await _reportService.GetServiceReportAsync(query));
    }

    /// <summary>
    /// Booked hours and their share for each tariff zone (morning, standard, peak, evening).
    /// Shows whether the discounts attract clients and how busy the peak is. Requires the admin API key.
    /// </summary>
    [HttpGet("tariff-zones")]
    [ProducesResponseType(typeof(List<TariffZoneReportItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<TariffZoneReportItem>>> GetTariffZoneReport([FromQuery] ReportPeriodQuery query)
    {
        return Ok(await _reportService.GetTariffZoneReportAsync(query));
    }
}