using System;

namespace TourMate.API.Models;

public class Complaint
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int TouristId { get; set; }
    public int GuideId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, resolved, dismissed
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public Booking? Booking { get; set; }
}