using ConferenceRoomBooking.Api.Dtos;

namespace ConferenceRoomBooking.Api.Services;

public interface IBookingService
{
    /// <exception cref="Exceptions.NotFoundException">Booking does not exist.</exception>
    Task<BookingResponse> GetByIdAsync(int id);

    /// <summary>Books a room for a time slot and calculates the total price.</summary>
    /// <exception cref="Exceptions.NotFoundException">Room does not exist.</exception>
    /// <exception cref="Exceptions.BadRequestException">Time slot breaks booking rules or a service is not available in the room.</exception>
    /// <exception cref="Exceptions.ConflictException">Room is already booked for this time.</exception>
    Task<BookingResponse> CreateAsync(BookingRequest request);
}