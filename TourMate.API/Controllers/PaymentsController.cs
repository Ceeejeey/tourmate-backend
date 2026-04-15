using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TourMate.API.Data;
using TourMate.API.Models;

namespace TourMate.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        int userId = 0;
        if (role != "admin")
        {
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out userId))
                return Unauthorized();
        }

        var query = _context.Payments
            .Include(p => p.Booking)
            .ThenInclude(b => b.Guide)
            .Include(p => p.Booking)
            .ThenInclude(b => b.Tourist)
            .AsQueryable();
            
        if (role == "tourist") {
            query = query.Where(p => p.Booking.TouristId == userId);
        } else if (role == "guide") {
            query = query.Where(p => p.Booking.GuideId == userId);
        } else if (role != "admin") {
            return StatusCode(403);
        }

        var payments = await query.OrderByDescending(p => p.Date)
            .Select(p => new {
                p.Id,
                p.BookingId,
                p.Amount,
                p.Date,
                p.Status,
                p.Method,
                Booking = new {
                    Id = p.Booking.Id,
                    GuideId = p.Booking.GuideId,
                    TouristId = p.Booking.TouristId,
                    Guide = new { Name = p.Booking.Guide.Name },
                    Tourist = new { Name = p.Booking.Tourist.Name }
                }
            })
            .ToListAsync();
        return Ok(payments);
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto request)
    {
        var booking = await _context.Bookings.FindAsync(request.BookingId);
        if (booking == null)
            return NotFound(new { message = "Booking not found" });

        if (booking.PaymentStatus == "paid")
            return BadRequest(new { message = "Booking is already paid" });

        // Simulate a payment sandbox processing
        // Any card detail is accepted 
        var isSimulationSuccessful = true;
        
        if (!isSimulationSuccessful)
            return BadRequest(new { message = "Payment failed simulation." });

        // Create Payment Record
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            Status = "completed",
            Method = "card_sandbox"
        };
        
        _context.Payments.Add(payment);

        // Update booking
        booking.PaymentStatus = "paid";
        if (booking.Status == "pending") {
             booking.Status = "confirmed"; // Auto-confirm if pending and paid
        }

        await _context.SaveChangesAsync();

        return Ok(new { 
           message = "Payment processed successfully", 
           paymentId = payment.Id,
           transactionId = Guid.NewGuid().ToString() // Mock transaction ID
        });
    }
}

public class ProcessPaymentDto 
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
}