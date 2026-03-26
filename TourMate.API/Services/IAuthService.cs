using System.Threading.Tasks;
using TourMate.API.DTOs;
using TourMate.API.Models;

namespace TourMate.API.Services;

public interface IAuthService
{
    Task<object> RegisterTouristAsync(TouristRegistrationDto dto);
    Task<object> RegisterGuideAsync(GuideRegistrationDto dto);
    Task<string> LoginAsync(UserLoginDto dto);
    Task<string> AdminLoginAsync(UserLoginDto dto);
}