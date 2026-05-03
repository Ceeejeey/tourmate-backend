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
public class ComplaintsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComplaintsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitComplaint([FromBody] CreateComplaintDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var userRole = User.FindFirstValue(ClaimTypes.Role);

        var booking = await _context.Bookings.FindAsync(dto.BookingId);
        if (booking == null) return NotFound(new { message = "Booking not found" });

        // Verify that the user was part of the booking
        if (userRole == "tourist" && booking.TouristId != userId)
            return StatusCode(403, new { message = "You can only complain about your own bookings" });
        if (userRole == "guide" && booking.GuideId != userId)
            return StatusCode(403, new { message = "You can only complain about your own bookings" });

        var complaint = new Complaint
        {
            BookingId = dto.BookingId,
            TouristId = booking.TouristId,
            GuideId = booking.GuideId,
            Reason = dto.Reason,
            Status = "pending",
            Date = DateTime.UtcNow
        };

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Complaint submitted successfully", id = complaint.Id });
    }

    [HttpGet("my-complaints")]
    public async Task<IActionResult> GetMyComplaints()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var userRole = User.FindFirstValue(ClaimTypes.Role);
        
        var query = _context.Complaints
            .Include(c => c.Booking)
            .ThenInclude(b => b.Tourist)
            .Include(c => c.Booking)
            .ThenInclude(b => b.Guide)
            .AsQueryable();

        if (userRole == "tourist")
        {
            query = query.Where(c => c.TouristId == userId);
        }
        else if (userRole == "guide")
        {
            query = query.Where(c => c.GuideId == userId);
        }
        else
        {
            return BadRequest(new { message = "Invalid role" });
        }

        var complaints = await query
            .OrderByDescending(c => c.Date)
            .Select(c => new {
                id = c.Id,
                bookingId = c.BookingId,
                touristId = c.TouristId,
                touristName = c.Booking!.Tourist!.Name,
                guideId = c.GuideId,
                guideName = c.Booking!.Guide!.Name,
                reason = c.Reason,
                status = c.Status,
                adminNote = c.AdminNote,
                date = c.Date
            })
            .ToListAsync();

        return Ok(complaints);
    }

    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllComplaints()
    {
        var complaints = await _context.Complaints
            .Include(c => c.Booking)
            .ThenInclude(b => b.Tourist)
            .Include(c => c.Booking)
            .ThenInclude(b => b.Guide)
            .OrderByDescending(c => c.Date)
            .Select(c => new {
                id = c.Id,
                bookingId = c.BookingId,
                touristId = c.TouristId,
                touristName = c.Booking!.Tourist!.Name,
                guideId = c.GuideId,
                guideName = c.Booking!.Guide!.Name,
                reason = c.Reason,
                status = c.Status,
                adminNote = c.AdminNote,
                date = c.Date
            })
            .ToListAsync();

        return Ok(complaints);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateComplaintStatusDto dto)
    {
        var complaint = await _context.Complaints.FindAsync(id);
        if (complaint == null) return NotFound(new { message = "Complaint not found" });

        var validStatuses = new[] { "pending", "resolved", "dismissed" };
        if (!validStatuses.Contains(dto.Status.ToLower()))
        {
            return BadRequest(new { message = "Invalid status. Must be pending, resolved, or dismissed" });
        }

        complaint.Status = dto.Status.ToLower();
        if (dto.AdminNote != null)
        {
            complaint.AdminNote = dto.AdminNote;
        }
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated successfully", status = complaint.Status, adminNote = complaint.AdminNote });
    }
}

public class CreateComplaintDto
{
    public int BookingId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class UpdateComplaintStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}