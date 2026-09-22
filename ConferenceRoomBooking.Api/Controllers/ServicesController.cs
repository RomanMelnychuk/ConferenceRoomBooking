using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Security;
using ConferenceRoomBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _serviceCatalog;

    public ServicesController(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    /// <summary>Returns the catalog of services that can be attached to rooms.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceResponse>>> GetAll()
    {
        return Ok(await _serviceCatalog.GetAllAsync());
    }

    /// <summary>Adds a new service to the catalog. Requires the admin API key.</summary>
    [HttpPost]
    [AdminApiKey]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceResponse>> Create(ServiceRequest request)
    {
        var service = await _serviceCatalog.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, service);
    }
}