namespace TourMate.API.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "tourist"; // tourist, guide, admin
    public string? Phone { get; set; }
    public string? Nationality { get; set; }
    public string? Avatar { get; set; }
    
    // Guide specific fields
    public string? Languages { get; set; } // Can be serialized as comma-separated or JSON
    public string? Experience { get; set; }
    public string? Skills { get; set; } // Can be serialized as comma-separated or JSON 
    public double? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public decimal? PricePerSession { get; set; }
    public bool? IsAvailable { get; set; }
    public string? ServiceArea { get; set; }
    public string? Bio { get; set; }
    public bool? Verified { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}