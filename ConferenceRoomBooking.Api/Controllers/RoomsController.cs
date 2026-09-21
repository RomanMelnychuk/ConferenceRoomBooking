using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>Returns all conference rooms with their available services.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoomResponse>>> GetAll()
    {
        return Ok(await _roomService.GetAllAsync());
    }

    /// <summary>Returns a single room by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponse>> GetById(int id)
    {
        return Ok(await _roomService.GetByIdAsync(id));
    }

    /// <summary>Creates a new conference room.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoomResponse>> Create(RoomRequest request)
    {
        var room = await _roomService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    /// <summary>Updates room data and replaces its list of available services.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponse>> Update(int id, RoomRequest request)
    {
        return Ok(await _roomService.UpdateAsync(id, request));
    }

    /// <summary>Deletes a room. Rooms that have bookings cannot be deleted.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return NoContent();
    }
}