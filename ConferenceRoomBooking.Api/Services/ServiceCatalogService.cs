using ConferenceRoomBooking.Api.Data;
using ConferenceRoomBooking.Api.Dtos;
using ConferenceRoomBooking.Api.Exceptions;
using ConferenceRoomBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Api.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _context;

    public ServiceCatalogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceResponse>> GetAllAsync()
    {
        var services = await _context.Services
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return services.Select(s => s.ToResponse()).ToList();
    }

    public async Task<ServiceResponse> CreateAsync(ServiceRequest request)
    {
        var name = request.Name.Trim();

        var nameTaken = await _context.Services.AnyAsync(s => s.Name == name);
        if (nameTaken)
            throw new ConflictException($"Service '{name}' already exists.");

        var service = new Service { Name = name, Price = request.Price };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return service.ToResponse();
    }
}