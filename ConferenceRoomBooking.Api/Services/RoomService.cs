using ConferenceRoomBooking.Api.Data;
using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Exceptions;
using ConferenceRoomBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Api.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoomResponse>> GetAllAsync()
    {
        var rooms = await _context.ConferenceRooms
            .Include(r => r.Services)
            .AsNoTracking()
            .ToListAsync();

        return rooms.Select(ToResponse).ToList();
    }

    public async Task<RoomResponse> GetByIdAsync(int id)
    {
        var room = await _context.ConferenceRooms
            .Include(r => r.Services)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        return ToResponse(room);
    }

    public async Task<RoomResponse> CreateAsync(RoomRequest request)
    {
        var services = await GetServicesByIdsAsync(request.ServiceIds);

        var room = new ConferenceRoom
        {
            Name = request.Name,
            Capacity = request.Capacity,
            BasePricePerHour = request.BasePricePerHour,
            Services = services
        };

        _context.ConferenceRooms.Add(room);
        await _context.SaveChangesAsync();

        return ToResponse(room);
    }

    public async Task<RoomResponse> UpdateAsync(int id, RoomRequest request)
    {
        var room = await _context.ConferenceRooms
            .Include(r => r.Services)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        var services = await GetServicesByIdsAsync(request.ServiceIds);

        room.Name = request.Name;
        room.Capacity = request.Capacity;
        room.BasePricePerHour = request.BasePricePerHour;

        // Replace the set of available services
        room.Services.Clear();
        foreach (var service in services)
        {
            room.Services.Add(service);
        }

        await _context.SaveChangesAsync();

        return ToResponse(room);
    }

    public async Task DeleteAsync(int id)
    {
        var room = await _context.ConferenceRooms.FindAsync(id)
            ?? throw new NotFoundException($"Room with id {id} was not found.");

        // Deleting a room with bookings would silently delete them too (cascade)
        // and break reports, so we forbid it
        var hasBookings = await _context.Bookings.AnyAsync(b => b.RoomId == id);
        if (hasBookings)
            throw new ConflictException("Room has bookings and cannot be deleted.");

        _context.ConferenceRooms.Remove(room);
        await _context.SaveChangesAsync();
    }

    // Loads services by ids and fails if any id is not in the catalog
    private async Task<List<Service>> GetServicesByIdsAsync(List<int> serviceIds)
    {
        var distinctIds = serviceIds.Distinct().ToList();

        var services = await _context.Services
            .Where(s => distinctIds.Contains(s.Id))
            .ToListAsync();

        var missingIds = distinctIds.Except(services.Select(s => s.Id)).ToList();
        if (missingIds.Count > 0)
            throw new BadRequestException($"Services not found: {string.Join(", ", missingIds)}.");

        return services;
    }

    private static RoomResponse ToResponse(ConferenceRoom room) => new()
    {
        Id = room.Id,
        Name = room.Name,
        Capacity = room.Capacity,
        BasePricePerHour = room.BasePricePerHour,
        Services = room.Services
            .Select(s => new ServiceResponse { Id = s.Id, Name = s.Name, Price = s.Price })
            .ToList()
    };
}