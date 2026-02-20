using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> ValidateUser(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return isValid ? user : null;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public async Task<User> CreateUser(string username, string password, UserRole role, int? departmentId = null)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role,
            DepartmentId = departmentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
