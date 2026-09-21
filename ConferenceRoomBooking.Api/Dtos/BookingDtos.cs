using System.ComponentModel.DataAnnotations;

namespace ConferenceRoomBooking.Api.Dtos;

public class BookingRequest
{
    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    /// <summary>Local start time in whole hours, e.g. 2026-10-01T10:00:00</summary>
    [Required]
    public DateTime? StartTime { get; set; }

    /// <summary>Duration in whole hours (max 17: from 06:00 to 23:00)</summary>
    [Range(1, 17)]
    public int DurationHours { get; set; }

    // Ids of services selected for this booking; each must be available in the room
    public List<int> ServiceIds { get; set; } = new();
}

public class BookingResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<ServiceResponse> Services { get; set; } = new();
    public decimal TotalPrice { get; set; }
}