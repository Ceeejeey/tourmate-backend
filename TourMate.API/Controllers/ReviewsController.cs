using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMate.API.Data;
using TourMate.API.Models;

namespace TourMate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var touristId))
            return Unauthorized();

        var booking = await _context.Bookings.FindAsync(dto.BookingId);
        if (booking == null) return NotFound(new { message = "Booking not found" });

        if (booking.TouristId != touristId) return StatusCode(403, new { message = "Cannot review for other's booking" });
        if (booking.Status != "completed" && booking.PaymentStatus != "paid") return BadRequest(new { message = "Can only review completed or fully paid bookings" });
        if (booking.IsReviewed) return BadRequest(new { message = "Booking is already reviewed" });

        var review = new Review
        {
            BookingId = dto.BookingId,
            GuideId = booking.GuideId,
            TouristId = touristId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            Date = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        booking.IsReviewed = true; // Mark as reviewed
        await _context.SaveChangesAsync();

        return Ok(new { message = "Review submitted successfully", reviewId = review.Id });
    }

    [HttpGet("tourist")]
    public async Task<IActionResult> GetTouristReviews()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var touristId))
            return Unauthorized();

        var reviews = await _context.Reviews
            .Include(r => r.Guide)
            .Where(r => r.TouristId == touristId)
            .OrderByDescending(r => r.Date)
            .Select(r => new {
                id = r.Id,
                guideId = r.GuideId,
                guideName = r.Guide.Name,
                rating = r.Rating,
                comment = r.Comment,
                createdAt = r.Date,
                bookingId = r.BookingId
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("guide")]
    public async Task<IActionResult> GetGuideReviews()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var guideId))
            return Unauthorized();

        var reviews = await _context.Reviews
            .Include(r => r.Tourist)
            .Where(r => r.GuideId == guideId)
            .OrderByDescending(r => r.Date)
            .Select(r => new {
                id = r.Id,
                touristId = r.TouristId,
                touristName = r.Tourist.Name,
                rating = r.Rating,
                comment = r.Comment,
                createdAt = r.Date,
                bookingId = r.BookingId
            })
            .ToListAsync();

        return Ok(reviews);
    }
}

public class CreateReviewDto
{
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}