namespace TourMate.API.DTOs;

public class CreateBookingDto
{
    public int GuideId { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}
