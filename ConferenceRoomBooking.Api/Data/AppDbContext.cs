using ConferenceRoomBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ConferenceRoom> ConferenceRooms { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Money columns: 18 digits in total, 2 of them after the decimal point
        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ConferenceRoom>()
            .Property(r => r.BasePricePerHour)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(18, 2);

        // Speeds up the most frequent query: "does this room have a booking that overlaps this slot?"
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.RoomId, b.StartTime, b.EndTime });
    }
}