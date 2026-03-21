using System;

namespace TourMate.API.Models;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "pending"; // pending, completed, failed
    public string Method { get; set; } = string.Empty;
    
    public Booking? Booking { get; set; }
}