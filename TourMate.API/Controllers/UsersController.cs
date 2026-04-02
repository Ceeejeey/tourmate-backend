using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TourMate.API.Data;
using TourMate.API.DTOs;
using TourMate.API.Models;

namespace TourMate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                statusCode = 401,
                message = "Unauthorized: Invalid or missing authentication token",
                error = "Unauthorized",
                timestamp = DateTime.UtcNow
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Phone,
            user.Nationality,
            user.Avatar,
            Languages = string.IsNullOrEmpty(user.Languages) ? new string[0] : user.Languages.Split(',', System.StringSplitOptions.RemoveEmptyEntries),
            user.Experience,
            Skills = string.IsNullOrEmpty(user.Skills) ? new string[0] : user.Skills.Split(',', System.StringSplitOptions.RemoveEmptyEntries),
            user.Rating,
            user.ReviewCount,
            user.PricePerSession,
            user.IsAvailable,
            user.ServiceArea,
            user.Bio,
            user.Verified,
            user.Latitude,
            user.Longitude
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                statusCode = 401,
                message = "Unauthorized: You must be logged in to update your profile",
                error = "Unauthorized",
                timestamp = DateTime.UtcNow
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new
        {
            statusCode = 404,
            message = "User not found",
            error = "NotFound",
            timestamp = DateTime.UtcNow
        });

        user.Name = dto.Name;
        user.Phone = dto.Phone;
        user.Nationality = dto.Nationality;
        user.Avatar = dto.Avatar;

        // Guide specific
        if (user.Role == "guide")
        {
            user.Languages = dto.Languages;
            user.Experience = dto.Experience;
            user.Skills = dto.Skills;
            user.PricePerSession = dto.PricePerSession;
            user.IsAvailable = dto.IsAvailable;
            user.ServiceArea = dto.ServiceArea;
            user.Bio = dto.Bio;
            user.Latitude = dto.Latitude;
            user.Longitude = dto.Longitude;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            message = "Profile updated successfully",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPut("me/status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                statusCode = 401,
                message = "Unauthorized: You must be logged in to update your status",
                error = "Unauthorized",
                timestamp = DateTime.UtcNow
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new
        {
            statusCode = 404,
            message = "User not found",
            error = "NotFound",
            timestamp = DateTime.UtcNow
        });

        if (user.Role != "guide") return StatusCode(403, new
        {
            statusCode = 403,
            message = "Forbidden: Only guides can update availability status",
            error = "Forbidden",
            timestamp = DateTime.UtcNow
        });

        user.IsAvailable = dto.IsAvailable;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            message = "Status updated successfully",
            isAvailable = user.IsAvailable,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPut("me/location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto dto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                statusCode = 401,
                message = "Unauthorized: You must be logged in to update your location",
                error = "Unauthorized",
                timestamp = DateTime.UtcNow
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new
        {
            statusCode = 404,
            message = "User not found",
            error = "NotFound",
            timestamp = DateTime.UtcNow
        });

        if (user.Role != "guide") return StatusCode(403, new
        {
            statusCode = 403,
            message = "Forbidden: Only guides can update location",
            error = "Forbidden",
            timestamp = DateTime.UtcNow
        });

        user.Latitude = dto.Latitude;
        user.Longitude = dto.Longitude;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            statusCode = 200,
            message = "Location updated successfully",
            latitude = user.Latitude,
            longitude = user.Longitude,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("guides")]
    public async Task<IActionResult> GetAllGuides()
    {
        var guides = await _context.Users
            .Where(u => u.Role == "guide")
            .Select(u => new
            {
                id = u.Id.ToString(),
                name = u.Name,
                email = u.Email,
                avatar = u.Avatar,
                serviceArea = u.ServiceArea,
                rating = u.Rating,
                reviewCount = u.ReviewCount,
                isAvailable = u.IsAvailable,
                verified = u.Verified,
                phone = u.Phone,
                experience = u.Experience,
                languages = string.IsNullOrEmpty(u.Languages) ? new string[0] : u.Languages.Split(',', System.StringSplitOptions.RemoveEmptyEntries),
                skills = string.IsNullOrEmpty(u.Skills) ? new string[0] : u.Skills.Split(',', System.StringSplitOptions.RemoveEmptyEntries),
                bio = u.Bio
            })
            .ToListAsync();

        return Ok(guides);
    }

    [HttpPut("guides/{id}/verify")]
    public async Task<IActionResult> ToggleGuideVerification(int id)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "admin")
        {
            return StatusCode(403, new { message = "Forbidden: Admin access required" });
        }

        var guide = await _context.Users.FindAsync(id);
        if (guide == null || guide.Role != "guide")
        {
            return NotFound(new { message = "Guide not found" });
        }

        guide.Verified = !(guide.Verified ?? false);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Verification status updated", verified = guide.Verified });
    }
}