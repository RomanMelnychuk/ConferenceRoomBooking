using ConferenceRoomBooking.Api.Dtos;

namespace ConferenceRoomBooking.Api.Services;

public interface IRoomService
{
    Task<List<RoomResponse>> GetAllAsync();

    /// <exception cref="Exceptions.NotFoundException">Room does not exist.</exception>
    Task<RoomResponse> GetByIdAsync(int id);

    /// <exception cref="Exceptions.BadRequestException">Some service ids are not in the catalog.</exception>
    Task<RoomResponse> CreateAsync(RoomRequest request);

    /// <summary>Updates room data and fully replaces its list of available services.</summary>
    /// <exception cref="Exceptions.NotFoundException">Room does not exist.</exception>
    /// <exception cref="Exceptions.BadRequestException">Some service ids are not in the catalog.</exception>
    Task<RoomResponse> UpdateAsync(int id, RoomRequest request);

    /// <summary>Deletes a room. Rooms that have bookings cannot be deleted.</summary>
    /// <exception cref="Exceptions.NotFoundException">Room does not exist.</exception>
    /// <exception cref="Exceptions.ConflictException">Room has bookings.</exception>
    Task DeleteAsync(int id);
}