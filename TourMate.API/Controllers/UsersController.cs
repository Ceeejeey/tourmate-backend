using System.Threading.Tasks;
using System.Security.Claims;
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
            return Unauthorized();

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
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found");

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

        return Ok(new { message = "Profile updated successfully" });
    }
}