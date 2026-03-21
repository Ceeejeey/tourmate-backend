using System.ComponentModel.DataAnnotations;

namespace TourMate.API.DTOs;

public class UserRegistrationDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public string Role { get; set; } = "tourist"; // tourist, guide, admin

    // Optional fields collected during registration
    public string? Phone { get; set; }
    
    // For Tourists
    public string? Nationality { get; set; }
    
    // For Guides
    public string? ServiceArea { get; set; }
    public string? Languages { get; set; }
    public string? Experience { get; set; }
}
