using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(IUserService userService, JwtTokenService jwtTokenService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = await _userService.RegisterAsync(request);
        var token = _jwtTokenService.GenerateToken(
            new Domain.Entities.User
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            });

        return CreatedAtAction(null, new AuthResponse { Token = token, User = user });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.LoginAsync(request);
        if (user is null)
            return Unauthorized("Invalid email or password");

        var token = _jwtTokenService.GenerateToken(
            new Domain.Entities.User
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            });

        return Ok(new AuthResponse { Token = token, User = user });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        return Ok(new { userId, email, name });
    }
}
