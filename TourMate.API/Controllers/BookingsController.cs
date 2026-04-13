using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMate.API.Data;
using TourMate.API.DTOs;
using TourMate.API.Models;

namespace TourMate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var touristId))
            return Unauthorized("Invalid user token");

        var tourist = await _context.Users.FindAsync(touristId);
        if (tourist == null || tourist.Role != "tourist")
            return StatusCode(403, new { message = "Only tourists can create bookings" });

        var guide = await _context.Users.FindAsync(dto.GuideId);
        if (guide == null || guide.Role != "guide")
            return NotFound(new { message = "Guide not found" });

        var booking = new Booking
        {
            TouristId = touristId,
            GuideId = dto.GuideId,
            BookingDate = DateTime.UtcNow,
            Status = "pending",
            TotalPrice = dto.TotalPrice,
            Notes = dto.Notes
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(new { 
            statusCode = 200,
            message = "Booking created successfully", 
            bookingId = booking.Id 
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyBookings()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var query = _context.Bookings
            .Include(b => b.Guide)
            .Include(b => b.Tourist)
            .AsQueryable();

        if (role == "tourist")
            query = query.Where(b => b.TouristId == userId);
        else if (role == "guide")
            query = query.Where(b => b.GuideId == userId);
        else if (role != "admin")
            return StatusCode(403);

        var bookings = await query
            .Select(b => new
            {
                b.Id,
                b.GuideId,
                b.TouristId,
                b.BookingDate,
                b.Status,
                b.PaymentStatus,
                b.IsReviewed,
                b.TotalPrice,
                b.Notes,
                Guide = new { b.Guide.Id, b.Guide.Name, b.Guide.Avatar, b.Guide.ServiceArea, b.Guide.Phone, b.Guide.Latitude, b.Guide.Longitude },
                Tourist = new { b.Tourist.Id, b.Tourist.Name, b.Tourist.Avatar, b.Tourist.Phone, b.Tourist.Latitude, b.Tourist.Longitude }
            })
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var booking = await _context.Bookings
            .Include(b => b.Guide)
            .Include(b => b.Tourist)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();
        if (booking.TouristId != userId && booking.GuideId != userId) return StatusCode(403);

        return Ok(booking);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        // Tourists can cancel 'pending' or 'confirmed' bookings
        if (role == "tourist" && booking.TouristId == userId)
        {
            if (dto.Status == "cancelled" && (booking.Status == "pending" || booking.Status == "confirmed"))
            {
                booking.Status = "cancelled";
            }
            else
            {
                return BadRequest(new { message = "Tourists can only cancel existing active bookings" });
            }
        }
        else if (role == "guide" && booking.GuideId == userId)
        {
            if (dto.Status == "confirmed" || dto.Status == "cancelled" || dto.Status == "completed")
            {
                booking.Status = dto.Status.ToLower();
            }
            else
            {
                return BadRequest(new { message = "Invalid status update" });
            }
        }
        else
        {
            return StatusCode(403, new { message = "You do not have permission to modify this booking" });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated successfully", status = booking.Status });
    }
}
