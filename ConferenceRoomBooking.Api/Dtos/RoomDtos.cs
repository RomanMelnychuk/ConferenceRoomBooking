using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBooking.Api.Dtos;

// Used both for creating and updating a room
public class RoomRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Capacity { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal BasePricePerHour { get; set; }

    // Ids of services from the catalog that are available in this room
    public List<int> ServiceIds { get; set; } = new();
}

public class RoomResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePricePerHour { get; set; }
    public List<ServiceResponse> Services { get; set; } = new();
}

public class ServiceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}