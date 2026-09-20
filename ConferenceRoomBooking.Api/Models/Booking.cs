namespace ConferenceRoomBooking.Api.Models;

public class Booking
{
	public int Id { get; set; }
	public int RoomId { get; set; }
	public ConferenceRoom Room { get; set; } = null!;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public decimal TotalPrice { get; set; }
	public ICollection<Service> Services { get; set; } = new List<Service>();

}
