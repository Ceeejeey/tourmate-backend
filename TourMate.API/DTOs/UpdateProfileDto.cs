using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TourMate.API.DTOs;

public class UpdateProfileDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Nationality { get; set; }
    public string? Avatar { get; set; }
    
    // Guide specific fields
    public string? Languages { get; set; } // Expecting comma separated or json array
    public string? Experience { get; set; }
    public string? Skills { get; set; }
    public decimal? PricePerSession { get; set; }
    public bool? IsAvailable { get; set; }
    public string? ServiceArea { get; set; }
    public string? Bio { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}