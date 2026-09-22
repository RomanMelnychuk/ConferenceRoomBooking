using ConferenceRoomBooking.Api.Models;

namespace ConferenceRoomBooking.Api.Dtos;

// The single place that converts a Service entity into its API representation
public static class ServiceMappings
{
    public static ServiceResponse ToResponse(this Service service) => new()
    {
        Id = service.Id,
        Name = service.Name,
        Price = service.Price
    };
}