using System;

namespace TourMate.API.Models;

public class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int GuideId { get; set; }
    public int TouristId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public Booking? Booking { get; set; }
    public User? Guide { get; set; }
    public User? Tourist { get; set; }
}