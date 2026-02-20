using AttendanceWeb.Attributes;
using AttendanceWeb.Data;
using AttendanceWeb.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Controllers.Api;

[ApiController]
[Route("api/students")]
[ApiAuthorize]
public class StudentApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public StudentApiController(ApplicationDbContext context)
    {
        _context = context;
    }

#if DEBUG
    [HttpPost("seed/{courseId}")]
    public async Task<ActionResult> SeedTestStudents(int courseId)
    {
        var course = await _context.Courses.Include(c => c.Stage).FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var existingCount = await _context.Students.CountAsync(s => s.StageId == course.StageId);
        if (existingCount > 0) return Ok(new { message = "Stage already has " + existingCount + " students." });

        var sid = course.StageId;
        var did = course.Stage.DepartmentId;

        _context.Students.Add(new Models.Domain.Student { StudentId = "10000001", FullName = "Ahmed Ali", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000002", FullName = "Sara Hassan", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000003", FullName = "Mohammed Ibrahim", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000004", FullName = "Fatima Karim", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000005", FullName = "Omar Nasser", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000006", FullName = "Noor Rashid", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000007", FullName = "Hassan Majeed", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000008", FullName = "Zahra Salim", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000009", FullName = "Yusuf Adel", StageId = sid, DepartmentId = did });
        _context.Students.Add(new Models.Domain.Student { StudentId = "10000010", FullName = "Maryam Fouad", StageId = sid, DepartmentId = did });

        _context.UnpaidStudents.Add(new Models.Domain.UnpaidStudent { StudentId = "10000003", DepartmentId = did });
        _context.UnpaidStudents.Add(new Models.Domain.UnpaidStudent { StudentId = "10000007", DepartmentId = did });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Seeded 10 test students (2 unpaid)." });
    }
#endif

    [HttpGet("{courseId}")]
    public async Task<ActionResult<List<StudentDto>>> GetStudentsForCourse(int courseId)
    {
        // Get authenticated user ID
        var userId = (int)HttpContext.Items["UserId"]!;

        var course = await _context.Courses
            .Include(c => c.Stage)
            .ThenInclude(s => s.Department)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return NotFound(new { message = "Course not found." });
        }

        // Verify lecturer owns this course
        if (course.LecturerId != userId)
        {
            return Forbid();
        }

        // Get all students in this course's stage
        var students = await _context.Students
            .Where(s => s.StageId == course.StageId)
            .Select(s => new StudentDto
            {
                StudentId = s.StudentId,
                FullName = s.FullName,
                IsUnpaid = _context.UnpaidStudents.Any(u => u.StudentId == s.StudentId && u.DepartmentId == course.Stage.DepartmentId)
            })
            .OrderBy(s => s.FullName)
            .ToListAsync();

        return Ok(students);
    }
}
