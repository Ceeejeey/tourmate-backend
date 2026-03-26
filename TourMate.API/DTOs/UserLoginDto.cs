using System.ComponentModel.DataAnnotations;

namespace TourMate.API.DTOs;

public class UserLoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string? Role { get; set; }
}