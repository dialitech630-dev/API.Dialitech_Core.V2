using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return CreatedAtAction(null, response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response is null)
            return Unauthorized("Invalid email or password");

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var caregiverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (caregiverId is null)
            return Unauthorized();

        var caregiver = await _authService.GetByIdAsync(caregiverId);
        if (caregiver is null)
            return NotFound();

        return Ok(caregiver);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var caregiverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (caregiverId is null)
            return Unauthorized();

        var result = await _authService.UpdateProfileAsync(caregiverId, request);
        return Ok(result);
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var caregiverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (caregiverId is null)
            return Unauthorized();

        await _authService.DeleteAccountAsync(caregiverId);
        return NoContent();
    }
}
