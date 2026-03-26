using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TourMate.API.DTOs;
using TourMate.API.Services;

namespace TourMate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register/tourist")]
    public async Task<IActionResult> RegisterTourist([FromForm] TouristRegistrationDto request)
    {
        try
        {
            var result = await _authService.RegisterTouristAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register/guide")]
    public async Task<IActionResult> RegisterGuide([FromForm] GuideRegistrationDto request)
    {
        try
        {
            var result = await _authService.RegisterGuideAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        try
        {
            var token = await _authService.LoginAsync(request);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] UserLoginDto request)
    {
        try
        {
            var token = await _authService.AdminLoginAsync(request);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}