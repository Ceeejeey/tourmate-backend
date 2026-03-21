using System.Threading.Tasks;
using TourMate.API.DTOs;
using TourMate.API.Models;

namespace TourMate.API.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(UserRegistrationDto dto);
    Task<string> LoginAsync(UserLoginDto dto);
}