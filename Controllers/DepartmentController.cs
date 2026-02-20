using AttendanceWeb.Data;
using AttendanceWeb.Models.Domain;
using AttendanceWeb.Models.ViewModels;
using AttendanceWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendanceWeb.Controllers;

[Authorize(Roles = "DepartmentUser")]
public class DepartmentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly ExcelService _excelService;

    public DepartmentController(ApplicationDbContext context, AuthService authService, ExcelService excelService)
    {
        _context = context;
        _authService = authService;
        _excelService = excelService;
    }

    private int GetDepartmentId()
    {
        var departmentId = User.FindFirstValue("DepartmentId");
        return int.Parse(departmentId ?? "0");
    }

    public async Task<IActionResult> Index()
    {
        var departmentId = GetDepartmentId();
        var department = await _context.Departments
            .Include(d => d.Stages)
            .ThenInclude(s => s.Courses)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        if (department == null)
        {
            return NotFound();
        }

        return View(department);
    }

    #region Stages

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStage(string name, int year)
    {
        var departmentId = GetDepartmentId();

        if (await _context.Stages.AnyAsync(s => s.DepartmentId == departmentId && s.Year == year))
        {
            TempData["ErrorMessage"] = $"Stage {year} already exists for this department.";
            return RedirectToAction(nameof(Index));
        }

        var stage = new Stage
        {
            Name = name,
            Year = year,
            DepartmentId = departmentId
        };

        _context.Stages.Add(stage);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Stage '{name}' created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStage(int id)
    {
        var departmentId = GetDepartmentId();
        var stage = await _context.Stages.FirstOrDefaultAsync(s => s.Id == id && s.DepartmentId == departmentId);

        if (stage == null)
        {
            return NotFound();
        }

        _context.Stages.Remove(stage);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Stage '{stage.Name}' deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Courses

    [HttpGet]
    public async Task<IActionResult> CreateCourse(int stageId)
    {
        var departmentId = GetDepartmentId();
        var stage = await _context.Stages
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == stageId && s.DepartmentId == departmentId);

        if (stage == null)
        {
            return NotFound();
        }

        var model = new CreateCourseViewModel
        {
            StageId = stageId,
            StageName = stage.Name
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var stage = await _context.Stages.FindAsync(model.StageId);
            model.StageName = stage?.Name ?? "";
            return View(model);
        }

        var departmentId = GetDepartmentId();

        if (await _context.Users.AnyAsync(u => u.Username == model.LecturerUsername))
        {
            ModelState.AddModelError("LecturerUsername", "Username already exists.");
            var stage = await _context.Stages.FindAsync(model.StageId);
            model.StageName = stage?.Name ?? "";
            return View(model);
        }

        var course = new Course
        {
            Name = model.CourseName,
            Code = model.CourseCode,
            StageId = model.StageId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Create lecturer account
        var lecturer = await _authService.CreateUser(
            model.LecturerUsername,
            model.LecturerPassword,
            UserRole.Lecturer,
            departmentId
        );

        // Assign lecturer to course
        course.LecturerId = lecturer.Id;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Course '{course.Name}' and lecturer account created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var departmentId = GetDepartmentId();
        var course = await _context.Courses
            .Include(c => c.Stage)
            .FirstOrDefaultAsync(c => c.Id == id && c.Stage.DepartmentId == departmentId);

        if (course == null)
        {
            return NotFound();
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Course '{course.Name}' deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetLecturerPassword(int courseId, string newPassword)
    {
        var departmentId = GetDepartmentId();
        var course = await _context.Courses
            .Include(c => c.Stage)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.Stage.DepartmentId == departmentId);

        if (course == null || course.LecturerId == null)
        {
            TempData["ErrorMessage"] = "Course or lecturer not found.";
            return RedirectToAction(nameof(Index));
        }

        var lecturer = await _context.Users.FindAsync(course.LecturerId);
        if (lecturer == null)
        {
            TempData["ErrorMessage"] = "Lecturer account not found.";
            return RedirectToAction(nameof(Index));
        }

        lecturer.PasswordHash = _authService.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Password for lecturer '{lecturer.Username}' has been reset successfully!";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Students

    [HttpGet]
    public async Task<IActionResult> Students()
    {
        var departmentId = GetDepartmentId();
        var students = await _context.Students
            .Include(s => s.Stage)
            .Where(s => s.DepartmentId == departmentId)
            .OrderBy(s => s.Stage.Year)
            .ThenBy(s => s.FullName)
            .ToListAsync();

        return View(students);
    }

    [HttpGet]
    public IActionResult UploadStudents()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
            return View();
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Please upload an Excel file (.xlsx).");
            return View();
        }

        try
        {
            var departmentId = GetDepartmentId();
            using var stream = file.OpenReadStream();
            var students = await _excelService.ParseStudentExcel(stream, departmentId);

            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully uploaded {students.Count} students!";
            return RedirectToAction(nameof(Students));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
            return View();
        }
    }

    [HttpGet]
    public IActionResult UploadUnpaidStudents()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadUnpaidStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
            return View();
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Please upload an Excel file (.xlsx).");
            return View();
        }

        try
        {
            var departmentId = GetDepartmentId();
            using var stream = file.OpenReadStream();
            var studentIds = await _excelService.ParseUnpaidExcel(stream, departmentId);

            // Clear old unpaid records for this department
            var oldRecords = await _context.UnpaidStudents
                .Where(u => u.DepartmentId == departmentId)
                .ToListAsync();
            _context.UnpaidStudents.RemoveRange(oldRecords);

            // Add new unpaid records
            var unpaidStudents = studentIds.Select(sid => new UnpaidStudent
            {
                StudentId = sid,
                DepartmentId = departmentId,
                MarkedUnpaidAt = DateTime.UtcNow
            }).ToList();

            _context.UnpaidStudents.AddRange(unpaidStudents);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully updated unpaid students list ({unpaidStudents.Count} students)!";
            return RedirectToAction(nameof(Students));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
            return View();
        }
    }

    #endregion

    #region Attendance Reports

    [HttpGet]
    public async Task<IActionResult> AttendanceReports(int? courseId)
    {
        var departmentId = GetDepartmentId();

        var courses = await _context.Courses
            .Include(c => c.Stage)
            .Where(c => c.Stage.DepartmentId == departmentId)
            .OrderBy(c => c.Stage.Year)
            .ThenBy(c => c.Name)
            .ToListAsync();

        ViewBag.Courses = courses;
        ViewBag.SelectedCourseId = courseId;

        if (!courseId.HasValue)
        {
            return View(new List<AttendanceSession>());
        }

        var sessions = await _context.AttendanceSessions
            .Include(a => a.Course)
            .ThenInclude(c => c.Stage)
            .Include(a => a.AttendanceRecords)
            .Include(a => a.Lecturer)
            .Where(a => a.CourseId == courseId.Value)
            .OrderByDescending(a => a.SessionDate)
            .ToListAsync();

        return View(sessions);
    }

    #endregion
}
