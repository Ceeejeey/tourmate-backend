using System;

namespace TourMate.API.Models;

public class Booking
{
    public int Id { get; set; }
    public int TouristId { get; set; }
    public int GuideId { get; set; }
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = "pending"; // pending, confirmed, cancelled, completed
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public User? Tourist { get; set; }
    public User? Guide { get; set; }
}