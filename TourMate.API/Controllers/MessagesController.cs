using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TourMate.API.Data;
using TourMate.API.Models;

namespace TourMate.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(currentUserIdStr, out int currentUserId))
            return Unauthorized();

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null) return NotFound("User not found");

        // Get users who either have a booking with the current user OR have exchanged messages
        var interactedUserIds = await _context.Messages
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        var bookingUserIds = new System.Collections.Generic.List<int>();
        if (currentUser.Role == "guide")
        {
            bookingUserIds = await _context.Bookings
                .Where(b => b.GuideId == currentUserId)
                .Select(b => b.TouristId)
                .Distinct()
                .ToListAsync();
        }
        else if (currentUser.Role == "tourist")
        {
            bookingUserIds = await _context.Bookings
                .Where(b => b.TouristId == currentUserId)
                .Select(b => b.GuideId)
                .Distinct()
                .ToListAsync();
        }

        var allRelevantUserIds = interactedUserIds.Concat(bookingUserIds).Distinct();

        var users = await _context.Users
            .Where(u => allRelevantUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Role,
                u.Avatar,
                u.Nationality
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{otherUserId}")]
    public async Task<IActionResult> GetMessages(int otherUserId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(currentUserIdStr, out int currentUserId))
            return Unauthorized();

        var messages = await _context.Messages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        // Mark unread messages as read
        var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.Read).ToList();
        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.Read = true;
            }
            await _context.SaveChangesAsync();
        }

        return Ok(messages);
    }
}
