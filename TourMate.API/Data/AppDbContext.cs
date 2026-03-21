using Microsoft.EntityFrameworkCore;
using TourMate.API.Models;

namespace TourMate.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Complaint> Complaints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .Property(u => u.PricePerSession)
            .HasColumnType("decimal(18,2)");
            
        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasColumnType("decimal(18,2)");
            
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Tourist)
            .WithMany()
            .HasForeignKey(b => b.TouristId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Guide)
            .WithMany()
            .HasForeignKey(b => b.GuideId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Message>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Guide)
            .WithMany()
            .HasForeignKey(r => r.GuideId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Tourist)
            .WithMany()
            .HasForeignKey(r => r.TouristId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Complaint>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.TouristId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Complaint>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.GuideId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}