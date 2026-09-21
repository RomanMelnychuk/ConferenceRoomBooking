using ConferenceRoomBooking.Api.Models;

namespace ConferenceRoomBooking.Api.Data
{
    public class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            var hasRooms = context.ConferenceRooms.Any();
            if (hasRooms) return;

            var projector = new Service { Name = "Projector", Price = 500m };
            var wifi = new Service { Name = "Wi-Fi", Price = 300m };
            var sound = new Service { Name = "Sound", Price = 700m };

            var allServices = new List<Service> { projector, wifi, sound };

            var rooms = new List<ConferenceRoom>
            {
                new() { Name = "Room A", Capacity = 50, BasePricePerHour = 2000m, Services = new List<Service>(allServices) },
                new() { Name = "Room B", Capacity = 100, BasePricePerHour = 3500m, Services = new List<Service>(allServices) },
                new() { Name = "Room C", Capacity = 30, BasePricePerHour = 1500m, Services = new List<Service>(allServices) }
            };

            context.ConferenceRooms.AddRange(rooms);
            context.SaveChanges();
        }
    }
}
