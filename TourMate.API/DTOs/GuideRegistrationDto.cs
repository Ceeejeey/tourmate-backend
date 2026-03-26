using System.ComponentModel.DataAnnotations;

namespace TourMate.API.DTOs;

public class GuideRegistrationDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required.")]
    public string Phone { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Service Area is required.")]
    public string ServiceArea { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Languages Spoken is required.")]
    public string Languages { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Experience and Skills are required.")]
    public string Experience { get; set; } = string.Empty;

    public Microsoft.AspNetCore.Http.IFormFile? ProfilePhoto { get; set; }
}