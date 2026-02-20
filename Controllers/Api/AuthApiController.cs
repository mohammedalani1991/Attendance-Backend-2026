using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using AttendanceWeb.Models.Dto;
using AttendanceWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthApiController(AuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var user = await _authService.ValidateUser(request.Username, request.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        if (user.Role != UserRole.Lecturer)
        {
            return Unauthorized(new { message = "Only lecturers can login to the mobile app." });
        }

        // Generate API token
        var token = Guid.NewGuid().ToString("N"); // 32-character hex string
        var apiToken = new ApiToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30) // 30-day expiry
        };

        _context.ApiTokens.Add(apiToken);
        await _context.SaveChangesAsync();

        // Get courses for this lecturer
        var courses = await _context.Courses
            .Include(c => c.Stage)
            .Where(c => c.LecturerId == user.Id)
            .Select(c => new CourseDto
            {
                CourseId = c.Id,
                CourseName = c.Name,
                CourseCode = c.Code,
                StageName = c.Stage.Name
            })
            .ToListAsync();

        var response = new LoginResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            Token = token,
            Courses = courses
        };

        return Ok(response);
    }
}
