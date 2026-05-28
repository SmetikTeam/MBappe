using MBappe.Common;
using MBappe.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public static class LearningDebugScenario
{
    public static async Task RunAsync(
        AuthService authService,
        UserManagementService userManagementService,
        EmployeeService employeeService,
        LearningService learningService,
        AuditLogService auditLogService)
    {
        Debug.WriteLine("=== Learning debug scenario started ===");

        var adminLogin = await authService.LoginAsync("admin", "12345");
        Debug.WriteLine($"Admin login: {adminLogin.Success} / {adminLogin.Message}");

        var manager = await EnsureUserAsync(
            userManagementService,
            "learning-manager",
            "learning-manager@mbappe.local",
            "Learning Manager",
            UserRole.Manager);

        var student = await EnsureUserAsync(
            userManagementService,
            "learning-student",
            "learning-student@mbappe.local",
            "Learning Student",
            UserRole.Employee);

        if (manager is null || student is null)
        {
            Debug.WriteLine("Learning users were not prepared. Scenario stopped.");
            return;
        }

        var managerProfile = await EnsureEmployeeAsync(
            employeeService,
            manager,
            "LRN-MGR-001",
            "Learning Manager",
            "Руководитель обучения",
            "HR",
            null);

        var studentProfile = await EnsureEmployeeAsync(
            employeeService,
            student,
            "LRN-STU-001",
            "Learning Student",
            "Специалист",
            "Поддержка",
            managerProfile?.Id);

        if (managerProfile is null || studentProfile is null)
        {
            Debug.WriteLine("Learning employee profiles were not prepared. Scenario stopped.");
            return;
        }

        var onboardingCourse = await EnsureActiveCourseAsync(
            learningService,
            "Адаптация нового сотрудника",
            "Базовый маршрут знакомства с регламентами и рабочими процессами",
            LearningFormat.Mixed,
            "MBappe HR",
            12);

        var securityCourse = await EnsureActiveCourseAsync(
            learningService,
            "Информационная безопасность",
            "Правила защиты данных, учетных записей и внутренних документов",
            LearningFormat.Online,
            "MBappe Security",
            6);

        if (onboardingCourse is null || securityCourse is null)
        {
            Debug.WriteLine("Learning courses were not prepared. Scenario stopped.");
            return;
        }

        var onboardingAssignment = await learningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = onboardingCourse.Id,
            EmployeeId = studentProfile.Id,
            DueDate = DateTime.Today.AddDays(21)
        });

        Debug.WriteLine($"Assign onboarding: {onboardingAssignment.Success} / {onboardingAssignment.Message}");

        if (onboardingAssignment.Assignment is not null)
        {
            var updateProgress = await learningService.UpdateAssignmentProgressAsync(new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = onboardingAssignment.Assignment.Id,
                ProgressPercent = 40
            });

            Debug.WriteLine($"Admin update onboarding progress: {updateProgress.Success} / {updateProgress.Message}");
        }

        var securityAssignment = await learningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = securityCourse.Id,
            EmployeeId = studentProfile.Id,
            DueDate = DateTime.Today.AddDays(14)
        });

        Debug.WriteLine($"Admin assign security: {securityAssignment.Success} / {securityAssignment.Message}");

        if (securityAssignment.Assignment is not null)
        {
            var completeSecurity = await learningService.UpdateAssignmentProgressAsync(new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = securityAssignment.Assignment.Id,
                ProgressPercent = 100,
                Score = 92
            });

            Debug.WriteLine($"Admin complete security: {completeSecurity.Success} / {completeSecurity.Message}");
        }

        await authService.LogoutAsync();

        var managerLogin = await authService.LoginAsync("learning-manager", "12345");
        Debug.WriteLine($"Manager login: {managerLogin.Success} / {managerLogin.Message}");

        var managerVisibleCourses = await learningService.GetVisibleCoursesAsync();
        Debug.WriteLine($"Manager visible subordinate courses: {managerVisibleCourses.Courses?.Count ?? 0}");

        var managerVisibleAssignments = await learningService.GetVisibleAssignmentsAsync();
        Debug.WriteLine($"Manager visible subordinate assignments: {managerVisibleAssignments.Assignments?.Count ?? 0}");

        var deniedManagerAssign = await learningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = securityCourse.Id,
            EmployeeId = studentProfile.Id,
            DueDate = DateTime.Today.AddDays(30)
        });

        Debug.WriteLine($"Manager assign denied: {!deniedManagerAssign.Success} / {deniedManagerAssign.Message}");

        if (onboardingAssignment.Assignment is not null)
        {
            var deniedManagerProgress = await learningService.UpdateAssignmentProgressAsync(new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = onboardingAssignment.Assignment.Id,
                ProgressPercent = 55
            });

            Debug.WriteLine($"Manager update progress denied: {!deniedManagerProgress.Success} / {deniedManagerProgress.Message}");
        }

        await authService.LogoutAsync();

        var studentLogin = await authService.LoginAsync("learning-student", "12345");
        Debug.WriteLine($"Student login: {studentLogin.Success} / {studentLogin.Message}");

        var visibleCourses = await learningService.GetVisibleCoursesAsync();
        Debug.WriteLine($"Student visible courses: {visibleCourses.Courses?.Count ?? 0}");

        var visibleAssignments = await learningService.GetVisibleAssignmentsAsync();
        Debug.WriteLine($"Student visible assignments: {visibleAssignments.Assignments?.Count ?? 0}");

        if (onboardingAssignment.Assignment is not null)
        {
            var studentProgress = await learningService.UpdateAssignmentProgressAsync(new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = onboardingAssignment.Assignment.Id,
                ProgressPercent = 75
            });

            Debug.WriteLine($"Student update own progress: {studentProgress.Success} / {studentProgress.Message}");
        }

        var deniedCourseCreate = await learningService.CreateCourseAsync(new CreateLearningCourseRequest
        {
            Title = "Запрещенный курс",
            Description = "Обычный сотрудник не должен создавать курсы",
            Format = LearningFormat.Online,
            Provider = "Denied",
            DurationHours = 1
        });

        Debug.WriteLine($"Student create course denied: {!deniedCourseCreate.Success} / {deniedCourseCreate.Message}");

        var auditEntries = await auditLogService.GetAllAsync();
        Debug.WriteLine($"Audit entries: {auditEntries.Count}");

        await authService.LogoutAsync();

        Debug.WriteLine("=== Learning debug scenario finished ===");
    }

    private static async Task<AppUser?> EnsureUserAsync(
        UserManagementService userManagementService,
        string login,
        string email,
        string fullName,
        UserRole role)
    {
        var users = await userManagementService.GetAllUsersAsync();
        var existingUser = users.Users?.FirstOrDefault(user => user.Login == login);

        if (existingUser is not null)
            return existingUser;

        var createUser = await userManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = login,
            Email = email,
            FullName = fullName,
            Password = "12345",
            ConfirmPassword = "12345",
            Role = role
        });

        Debug.WriteLine($"Create {login}: {createUser.Success} / {createUser.Message}");

        return createUser.User;
    }

    private static async Task<EmployeeProfile?> EnsureEmployeeAsync(
        EmployeeService employeeService,
        AppUser user,
        string personnelNumber,
        string fullName,
        string position,
        string department,
        Guid? managerEmployeeId)
    {
        var employees = await employeeService.GetAllEmployeesAsync();
        var existingEmployee = employees.Employees?.FirstOrDefault(employee => employee.UserId == user.Id);

        if (existingEmployee is not null)
            return existingEmployee;

        var createEmployee = await employeeService.CreateEmployeeAsync(new CreateEmployeeRequest
        {
            UserId = user.Id,
            PersonnelNumber = personnelNumber,
            FullName = fullName,
            Position = position,
            Department = department,
            ManagerEmployeeId = managerEmployeeId,
            Email = user.Email,
            Phone = "+7 900 000-00-00",
            HireDate = DateTime.Today
        });

        Debug.WriteLine($"Create employee {personnelNumber}: {createEmployee.Success} / {createEmployee.Message}");

        return createEmployee.Employee;
    }

    private static async Task<LearningCourse?> EnsureActiveCourseAsync(
        LearningService learningService,
        string title,
        string description,
        LearningFormat format,
        string provider,
        double durationHours)
    {
        var courses = await learningService.GetVisibleCoursesAsync();
        var existingCourse = courses.Courses?.FirstOrDefault(course => course.Title == title);

        if (existingCourse is null)
        {
            var createCourse = await learningService.CreateCourseAsync(new CreateLearningCourseRequest
            {
                Title = title,
                Description = description,
                Format = format,
                Provider = provider,
                DurationHours = durationHours
            });

            Debug.WriteLine($"Create course {title}: {createCourse.Success} / {createCourse.Message}");
            existingCourse = createCourse.Course;
        }

        if (existingCourse is null)
            return null;

        if (existingCourse.Status == LearningCourseStatus.Active)
            return existingCourse;

        var updateCourse = await learningService.UpdateCourseAsync(new UpdateLearningCourseRequest
        {
            CourseId = existingCourse.Id,
            Title = existingCourse.Title,
            Description = existingCourse.Description,
            Format = existingCourse.Format,
            Provider = existingCourse.Provider,
            DurationHours = existingCourse.DurationHours,
            Status = LearningCourseStatus.Active
        });

        Debug.WriteLine($"Activate course {title}: {updateCourse.Success} / {updateCourse.Message}");

        return updateCourse.Course;
    }
}
