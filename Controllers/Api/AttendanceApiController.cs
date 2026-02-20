using AttendanceWeb.Attributes;
using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using AttendanceWeb.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceWeb.Controllers.Api;

[ApiController]
[Route("api/attendance")]
[ApiAuthorize]
public class AttendanceApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AttendanceApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("session")]
    public async Task<IActionResult> UploadSession([FromBody] AttendanceSessionDto sessionDto)
    {
        // Get authenticated user ID
        var userId = (int)HttpContext.Items["UserId"]!;

        if (sessionDto == null || sessionDto.ScannedStudents == null)
        {
            return BadRequest(new { message = "Invalid session data." });
        }

        var course = await _context.Courses
            .Include(c => c.Stage)
            .ThenInclude(s => s.Students)
            .FirstOrDefaultAsync(c => c.Id == sessionDto.CourseId);

        if (course == null)
        {
            return NotFound(new { message = "Course not found." });
        }

        if (!course.LecturerId.HasValue)
        {
            return BadRequest(new { message = "No lecturer assigned to this course." });
        }

        // Verify lecturer owns this course
        if (course.LecturerId.Value != userId)
        {
            return Forbid();
        }

        // Create attendance session
        var session = new AttendanceSession
        {
            CourseId = sessionDto.CourseId,
            LecturerId = course.LecturerId.Value,
            SessionDate = sessionDto.SessionDate,
            StartedAt = sessionDto.StartedAt,
            EndedAt = sessionDto.EndedAt,
            TotalScanned = sessionDto.ScannedStudents.Count,
            UploadedAt = DateTime.UtcNow
        };

        _context.AttendanceSessions.Add(session);
        await _context.SaveChangesAsync();

        // Get unpaid student IDs for this department
        var unpaidStudentIds = await _context.UnpaidStudents
            .Where(u => u.DepartmentId == course.Stage.DepartmentId)
            .Select(u => u.StudentId)
            .ToHashSetAsync();

        // Get all students in this stage
        var stageStudentIds = course.Stage.Students.Select(s => s.StudentId).ToHashSet();

        // Create attendance records for scanned students
        var scannedStudentIds = new HashSet<string>();
        foreach (var scanned in sessionDto.ScannedStudents)
        {
            if (!stageStudentIds.Contains(scanned.StudentId))
            {
                continue; // Skip invalid student IDs
            }

            scannedStudentIds.Add(scanned.StudentId);

            var record = new AttendanceRecord
            {
                AttendanceSessionId = session.Id,
                StudentId = scanned.StudentId,
                ScannedAt = scanned.ScannedAt,
                IsPresent = true,
                IsUnpaid = unpaidStudentIds.Contains(scanned.StudentId)
            };

            _context.AttendanceRecords.Add(record);
        }

        // Mark unscanned students as absent
        foreach (var student in course.Stage.Students)
        {
            if (!scannedStudentIds.Contains(student.StudentId))
            {
                var record = new AttendanceRecord
                {
                    AttendanceSessionId = session.Id,
                    StudentId = student.StudentId,
                    ScannedAt = sessionDto.EndedAt,
                    IsPresent = false,
                    IsUnpaid = unpaidStudentIds.Contains(student.StudentId)
                };

                _context.AttendanceRecords.Add(record);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Session uploaded successfully.",
            sessionId = session.Id,
            totalStudents = course.Stage.Students.Count,
            presentCount = scannedStudentIds.Count,
            absentCount = course.Stage.Students.Count - scannedStudentIds.Count
        });
    }

    [HttpGet("sessions/{lecturerId}")]
    public async Task<IActionResult> GetSessions(int lecturerId)
    {
        // Get authenticated user ID
        var userId = (int)HttpContext.Items["UserId"]!;

        // Verify lecturer is requesting their own sessions
        if (lecturerId != userId)
        {
            return Forbid();
        }

        var sessions = await _context.AttendanceSessions
            .Where(s => s.LecturerId == lecturerId)
            .Include(s => s.Course)
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new
            {
                s.Id,
                s.CourseId,
                courseName = s.Course.Name,
                courseCode = s.Course.Code,
                s.SessionDate,
                s.StartedAt,
                s.EndedAt,
                s.TotalScanned,
                s.UploadedAt,
                totalStudents = _context.AttendanceRecords.Count(r => r.AttendanceSessionId == s.Id),
                presentCount = _context.AttendanceRecords.Count(r => r.AttendanceSessionId == s.Id && r.IsPresent),
                absentCount = _context.AttendanceRecords.Count(r => r.AttendanceSessionId == s.Id && !r.IsPresent)
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("session/{sessionId}/records")]
    public async Task<IActionResult> GetSessionRecords(int sessionId)
    {
        // Get authenticated user ID
        var userId = (int)HttpContext.Items["UserId"]!;

        // Verify lecturer owns this session
        var session = await _context.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound(new { message = "Session not found." });
        }

        if (session.LecturerId != userId)
        {
            return Forbid();
        }

        var records = await _context.AttendanceRecords
            .Where(r => r.AttendanceSessionId == sessionId)
            .OrderByDescending(r => r.IsPresent)
            .ThenBy(r => r.StudentId)
            .Select(r => new
            {
                r.StudentId,
                fullName = _context.Students.Where(s => s.StudentId == r.StudentId).Select(s => s.FullName).FirstOrDefault() ?? "Unknown",
                r.IsPresent,
                r.IsUnpaid,
                r.ScannedAt
            })
            .ToListAsync();

        return Ok(records);
    }
}
