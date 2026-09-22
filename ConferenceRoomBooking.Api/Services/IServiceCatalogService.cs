using ConferenceRoomBooking.Api.Dtos;

namespace ConferenceRoomBooking.Api.Services;

public interface IServiceCatalogService
{
    Task<List<ServiceResponse>> GetAllAsync();

    /// <summary>Adds a new service to the catalog, so it can be attached to rooms.</summary>
    /// <exception cref="Exceptions.ConflictException">A service with the same name already exists.</exception>
    Task<ServiceResponse> CreateAsync(ServiceRequest request);
}