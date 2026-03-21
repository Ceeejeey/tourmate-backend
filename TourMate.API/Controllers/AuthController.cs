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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(new { message = result });
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