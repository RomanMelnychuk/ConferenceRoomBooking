using System.Data;
using ConferenceRoomBooking.Api.Data;
using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Exceptions;
using ConferenceRoomBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly PriceCalculator _priceCalculator;

    public BookingService(AppDbContext context, PriceCalculator priceCalculator)
    {
        _context = context;
        _priceCalculator = priceCalculator;
    }

    public async Task<BookingResponse> GetByIdAsync(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.Services)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Booking with id {id} was not found.");

        return ToResponse(booking);
    }

    public async Task<BookingResponse> CreateAsync(BookingRequest request)
    {
        var start = request.StartTime!.Value;
        var end = start.AddHours(request.DurationHours);

        BookingTimeRules.Validate(start, end);

        // Database retries are enabled, so the transaction runs inside the execution strategy:
        // on a short failure the whole operation (check + insert) is repeated from scratch
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateInTransactionAsync(request, start, end));
    }

    private async Task<BookingResponse> CreateInTransactionAsync(BookingRequest request, DateTime start, DateTime end)
    {
        // Forget entities left from a failed previous attempt, so a retry does not insert twice
        _context.ChangeTracker.Clear();

        // Serializable isolation makes "check the slot is free" and "insert the booking"
        // one atomic operation, so two parallel requests cannot book the same slot
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var room = await _context.ConferenceRooms
            .Include(r => r.Services)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId)
            ?? throw new NotFoundException($"Room with id {request.RoomId} was not found.");

        var selectedServices = GetSelectedServices(room, request.ServiceIds);

        var isTaken = await _context.Bookings
            .AnyAsync(b => b.RoomId == room.Id && b.StartTime < end && start < b.EndTime);
        if (isTaken)
            throw new ConflictException("The room is already booked for this time.");

        var booking = new Booking
        {
            RoomId = room.Id,
            Room = room,
            StartTime = start,
            EndTime = end,
            Services = selectedServices,
            TotalPrice = _priceCalculator.CalculateTotal(room.BasePricePerHour, start, end, selectedServices)
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(booking);
    }

    // Returns the requested services, but only if the room actually offers all of them
    private static List<Service> GetSelectedServices(ConferenceRoom room, List<int> serviceIds)
    {
        var distinctIds = serviceIds.Distinct().ToList();

        var unavailableIds = distinctIds.Except(room.Services.Select(s => s.Id)).ToList();
        if (unavailableIds.Count > 0)
            throw new BadRequestException($"Services not available in this room: {string.Join(", ", unavailableIds)}.");

        return room.Services.Where(s => distinctIds.Contains(s.Id)).ToList();
    }

    private static BookingResponse ToResponse(Booking booking) => new()
    {
        Id = booking.Id,
        RoomId = booking.RoomId,
        RoomName = booking.Room.Name,
        StartTime = booking.StartTime,
        EndTime = booking.EndTime,
        Services = booking.Services
            .Select(s => new ServiceResponse { Id = s.Id, Name = s.Name, Price = s.Price })
            .ToList(),
        TotalPrice = booking.TotalPrice
    };
}