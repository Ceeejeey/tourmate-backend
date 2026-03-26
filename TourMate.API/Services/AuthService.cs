using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TourMate.API.Data;
using TourMate.API.DTOs;
using TourMate.API.Models;

namespace TourMate.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(AppDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task<string?> SaveProfilePhotoAsync(IFormFile? photo)
    {
        if (photo == null || photo.Length == 0)
            return null;

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(fileStream);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";
        
        return $"{baseUrl}/uploads/profiles/{uniqueFileName}";
    }

    public async Task<object> RegisterTouristAsync(TouristRegistrationDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var photoUrl = await SaveProfilePhotoAsync(dto.ProfilePhoto);

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "tourist",
            Phone = dto.Phone,
            Nationality = dto.Nationality,
            Avatar = photoUrl
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            phone = user.Phone,
            nationality = user.Nationality,
            avatar = user.Avatar,
            message = "Tourist registered successfully"
        };
    }

    public async Task<object> RegisterGuideAsync(GuideRegistrationDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var photoUrl = await SaveProfilePhotoAsync(dto.ProfilePhoto);

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "guide",
            Phone = dto.Phone,
            ServiceArea = dto.ServiceArea,
            Languages = dto.Languages,
            Experience = dto.Experience,
            Avatar = photoUrl
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            phone = user.Phone,
            serviceArea = user.ServiceArea,
            languages = user.Languages,
            experience = user.Experience,
            avatar = user.Avatar,
            message = "Guide registered successfully"
        };
    }

    public async Task<string> LoginAsync(UserLoginDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new Exception("Invalid email or password");
        }

        if (user.Role == "admin") 
        {
            throw new Exception("Please use the admin login portal.");
        }
        
        if (!string.IsNullOrEmpty(dto.Role) && dto.Role.ToLower() != user.Role.ToLower())
        {
            throw new Exception($"Invalid role selected. This account is registered as a {user.Role}.");
        }

        return GenerateJwtToken(user);
    }

    public async Task<string> AdminLoginAsync(UserLoginDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new Exception("Invalid email or password");
        }

        if (user.Role != "admin") 
        {
            throw new Exception("Unauthorized. Admin access only.");
        }

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role), // Could be Admin, Tourist, Guide
            new Claim(ClaimTypes.Name, user.Name)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
