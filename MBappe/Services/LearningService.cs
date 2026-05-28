using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public class LearningService
{
    private readonly ILearningRepository _learningRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public LearningService(
        ILearningRepository learningRepository,
        IEmployeeRepository employeeRepository,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _learningRepository = learningRepository;
        _employeeRepository = employeeRepository;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public bool CanManageCourses()
    {
        return _sessionService.HasAnyRole(
            UserRole.Administrator,
            UserRole.HrSpecialist);
    }

    public bool CanAssignLearning()
    {
        return _sessionService.HasAnyRole(
            UserRole.Administrator,
            UserRole.HrSpecialist,
            UserRole.Manager);
    }

    public async Task<LearningOperationResult> GetVisibleCoursesAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить курсы без активной сессии", "Пользователь не авторизован");

        var courses = await _learningRepository.GetAllCoursesAsync();

        if (currentUser.Role is not (UserRole.Administrator or UserRole.HrSpecialist))
        {
            courses = courses
                .Where(course => course.Status == LearningCourseStatus.Active)
                .ToList();
        }

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseViewed,
            true,
            "Получен список курсов обучения",
            $"Количество курсов: {courses.Count}");

        return LearningOperationResult.Ok(courses, "Список курсов получен");
    }

    public async Task<LearningOperationResult> GetCourseByIdAsync(Guid courseId)
    {
        if (courseId == Guid.Empty)
            return LearningOperationResult.Fail("Не указан курс");

        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить курс без активной сессии", "Пользователь не авторизован");

        var course = await _learningRepository.GetCourseByIdAsync(courseId);

        if (course is null)
            return LearningOperationResult.Fail("Курс не найден");

        if (course.Status != LearningCourseStatus.Active && !CanManageCourses())
            return await AccessDeniedAsync("Попытка просмотреть недоступный курс обучения", "Недостаточно прав для просмотра курса");

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseViewed,
            true,
            "Получен курс обучения",
            $"Курс: {course.Title}");

        return LearningOperationResult.Ok(course, "Курс получен");
    }

    public async Task<LearningOperationResult> GetVisibleAssignmentsAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить назначения обучения без активной сессии", "Пользователь не авторизован");

        var allAssignments = await _learningRepository.GetAllAssignmentsAsync();
        IReadOnlyList<LearningAssignment> visibleAssignments;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
        {
            visibleAssignments = allAssignments;
        }
        else
        {
            var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

            if (currentEmployee is null)
                return LearningOperationResult.Fail("Для текущей учетной записи не создан профиль сотрудника");

            if (currentUser.Role == UserRole.Manager)
            {
                var visibleEmployeeIds = await GetVisibleEmployeeIdsForManagerAsync(currentEmployee.Id);

                visibleAssignments = allAssignments
                    .Where(assignment => visibleEmployeeIds.Contains(assignment.EmployeeId))
                    .ToList();
            }
            else
            {
                visibleAssignments = allAssignments
                    .Where(assignment => assignment.EmployeeId == currentEmployee.Id)
                    .ToList();
            }
        }

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseViewed,
            true,
            "Получен список назначений обучения",
            $"Количество назначений: {visibleAssignments.Count}");

        return LearningOperationResult.Ok(visibleAssignments, "Список назначений получен");
    }

    public async Task<LearningOperationResult> GetAssignmentByIdAsync(Guid assignmentId)
    {
        if (assignmentId == Guid.Empty)
            return LearningOperationResult.Fail("Не указано назначение обучения");

        var assignment = await _learningRepository.GetAssignmentByIdAsync(assignmentId);

        if (assignment is null)
            return LearningOperationResult.Fail("Назначение обучения не найдено");

        if (!await CanViewAssignmentAsync(assignment))
            return await AccessDeniedAsync("Попытка просмотреть назначение обучения без прав", "Недостаточно прав для просмотра назначения");

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseViewed,
            true,
            "Получено назначение обучения",
            $"Назначение: {assignment.Id}");

        return LearningOperationResult.Ok(assignment, "Назначение обучения получено");
    }

    public async Task<LearningOperationResult> CreateCourseAsync(CreateLearningCourseRequest request)
    {
        if (!CanManageCourses())
            return await AccessDeniedAsync("Попытка создать курс обучения", "Недостаточно прав для создания курса");

        var validationError = ValidateCourseFields(
            request.Title,
            request.Description,
            request.Provider,
            request.DurationHours);

        if (validationError is not null)
            return LearningOperationResult.Fail(validationError);

        var course = new LearningCourse
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Format = request.Format,
            Provider = request.Provider.Trim(),
            DurationHours = request.DurationHours,
            Status = LearningCourseStatus.Draft,
            CreatedAt = DateTime.Now
        };

        await _learningRepository.AddCourseAsync(course);

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseCreated,
            true,
            "Создан курс обучения",
            $"Курс: {course.Title}");

        return LearningOperationResult.Ok(course, "Курс обучения создан");
    }

    public async Task<LearningOperationResult> UpdateCourseAsync(UpdateLearningCourseRequest request)
    {
        if (!CanManageCourses())
            return await AccessDeniedAsync("Попытка изменить курс обучения", "Недостаточно прав для изменения курса");

        if (request.CourseId == Guid.Empty)
            return LearningOperationResult.Fail("Не указан курс");

        var course = await _learningRepository.GetCourseByIdAsync(request.CourseId);

        if (course is null)
            return LearningOperationResult.Fail("Курс не найден");

        var validationError = ValidateCourseFields(
            request.Title,
            request.Description,
            request.Provider,
            request.DurationHours);

        if (validationError is not null)
            return LearningOperationResult.Fail(validationError);

        course.Title = request.Title.Trim();
        course.Description = request.Description.Trim();
        course.Format = request.Format;
        course.Provider = request.Provider.Trim();
        course.DurationHours = request.DurationHours;
        course.Status = request.Status;
        course.UpdatedAt = DateTime.Now;

        await _learningRepository.UpdateCourseAsync(course);

        await _auditLogService.LogAsync(
            AuditActionType.LearningCourseUpdated,
            true,
            "Изменен курс обучения",
            $"Курс: {course.Title}, статус: {course.Status}");

        return LearningOperationResult.Ok(course, "Курс обучения обновлен");
    }

    public async Task<LearningOperationResult> AssignCourseAsync(AssignLearningCourseRequest request)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка назначить обучение без активной сессии", "Пользователь не авторизован");

        if (!CanAssignLearning())
            return await AccessDeniedAsync("Попытка назначить обучение без прав", "Недостаточно прав для назначения обучения");

        var course = await _learningRepository.GetCourseByIdAsync(request.CourseId);

        if (course is null)
            return LearningOperationResult.Fail("Курс не найден");

        if (course.Status != LearningCourseStatus.Active)
            return LearningOperationResult.Fail("Назначать можно только активный курс");

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null)
            return LearningOperationResult.Fail("Сотрудник не найден");

        if (employee.Status == EmployeeStatus.Dismissed)
            return LearningOperationResult.Fail("Нельзя назначить обучение уволенному сотруднику");

        if (!await CanAssignToEmployeeAsync(employee))
            return await AccessDeniedAsync("Попытка назначить обучение сотруднику без прав", "Недостаточно прав для назначения обучения этому сотруднику");

        if (request.DueDate is not null && request.DueDate.Value.Date < DateTime.Today)
            return LearningOperationResult.Fail("Срок обучения не может быть в прошлом");

        var existingAssignment = await _learningRepository.GetAssignmentAsync(course.Id, employee.Id);

        if (existingAssignment is not null && existingAssignment.Status != LearningAssignmentStatus.Cancelled)
            return LearningOperationResult.Fail("Этот курс уже назначен сотруднику");

        var assignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employee.Id,
            AssignedByUserId = currentUser.Id,
            AssignedAt = DateTime.Now,
            DueDate = request.DueDate?.Date,
            Status = LearningAssignmentStatus.Assigned,
            CreatedAt = DateTime.Now
        };

        await _learningRepository.AddAssignmentAsync(assignment);

        await _auditLogService.LogAsync(
            AuditActionType.LearningAssigned,
            true,
            "Назначено обучение",
            $"Курс: {course.Title}, сотрудник: {employee.FullName}");

        return LearningOperationResult.Ok(assignment, "Обучение назначено");
    }

    public async Task<LearningOperationResult> UpdateAssignmentProgressAsync(UpdateLearningAssignmentProgressRequest request)
    {
        var assignment = await _learningRepository.GetAssignmentByIdAsync(request.AssignmentId);

        if (assignment is null)
            return LearningOperationResult.Fail("Назначение обучения не найдено");

        if (assignment.Status == LearningAssignmentStatus.Cancelled)
            return LearningOperationResult.Fail("Нельзя обновлять отмененное обучение");

        if (!await CanUpdateAssignmentAsync(assignment))
            return await AccessDeniedAsync("Попытка обновить прогресс обучения без прав", "Недостаточно прав для обновления прогресса");

        if (request.ProgressPercent < 0 || request.ProgressPercent > 100)
            return LearningOperationResult.Fail("Прогресс должен быть от 0 до 100");

        if (request.Score is < 0 or > 100)
            return LearningOperationResult.Fail("Оценка должна быть от 0 до 100");

        assignment.ProgressPercent = request.ProgressPercent;
        assignment.Score = request.Score;
        assignment.UpdatedAt = DateTime.Now;

        RecalculateAssignmentStatus(assignment);

        await _learningRepository.UpdateAssignmentAsync(assignment);

        await _auditLogService.LogAsync(
            AuditActionType.LearningProgressUpdated,
            true,
            "Обновлен прогресс обучения",
            $"Назначение: {assignment.Id}, прогресс: {assignment.ProgressPercent}%");

        return LearningOperationResult.Ok(assignment, "Прогресс обучения обновлен");
    }

    public async Task<LearningOperationResult> CancelAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _learningRepository.GetAssignmentByIdAsync(assignmentId);

        if (assignment is null)
            return LearningOperationResult.Fail("Назначение обучения не найдено");

        if (assignment.Status == LearningAssignmentStatus.Cancelled)
            return LearningOperationResult.Fail("Обучение уже отменено");

        if (!await CanCancelAssignmentAsync(assignment))
            return await AccessDeniedAsync("Попытка отменить обучение без прав", "Недостаточно прав для отмены обучения");

        assignment.Status = LearningAssignmentStatus.Cancelled;
        assignment.UpdatedAt = DateTime.Now;

        await _learningRepository.UpdateAssignmentAsync(assignment);

        await _auditLogService.LogAsync(
            AuditActionType.LearningAssignmentCancelled,
            true,
            "Отменено назначение обучения",
            $"Назначение: {assignment.Id}");

        return LearningOperationResult.Ok(assignment, "Обучение отменено");
    }

    private async Task<HashSet<Guid>> GetVisibleEmployeeIdsForManagerAsync(Guid managerEmployeeId)
    {
        var employees = await _employeeRepository.GetAllAsync();

        return employees
            .Where(employee => employee.Id == managerEmployeeId || employee.ManagerEmployeeId == managerEmployeeId)
            .Select(employee => employee.Id)
            .ToHashSet();
    }

    private async Task<bool> CanViewAssignmentAsync(LearningAssignment assignment)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return false;

        if (assignment.EmployeeId == currentEmployee.Id)
            return true;

        return currentUser.Role == UserRole.Manager
            && await IsDirectReportAsync(assignment.EmployeeId, currentEmployee.Id);
    }

    private async Task<bool> CanAssignToEmployeeAsync(EmployeeProfile employee)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        if (currentUser.Role != UserRole.Manager)
            return false;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return false;

        return employee.ManagerEmployeeId == currentEmployee.Id;
    }

    private async Task<bool> CanUpdateAssignmentAsync(LearningAssignment assignment)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return false;

        if (assignment.EmployeeId == currentEmployee.Id)
            return true;

        return currentUser.Role == UserRole.Manager
            && await IsDirectReportAsync(assignment.EmployeeId, currentEmployee.Id);
    }

    private async Task<bool> CanCancelAssignmentAsync(LearningAssignment assignment)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        if (currentUser.Role != UserRole.Manager)
            return false;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        return currentEmployee is not null
            && await IsDirectReportAsync(assignment.EmployeeId, currentEmployee.Id);
    }

    private async Task<bool> IsDirectReportAsync(Guid employeeId, Guid managerEmployeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        return employee?.ManagerEmployeeId == managerEmployeeId;
    }

    private static string? ValidateCourseFields(
        string title,
        string description,
        string provider,
        double durationHours)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Введите название курса";

        if (title.Trim().Length < 3)
            return "Название курса должно содержать минимум 3 символа";

        if (string.IsNullOrWhiteSpace(description))
            return "Введите описание курса";

        if (string.IsNullOrWhiteSpace(provider))
            return "Введите провайдера обучения";

        if (durationHours <= 0)
            return "Длительность курса должна быть больше нуля";

        return null;
    }

    private static void RecalculateAssignmentStatus(LearningAssignment assignment)
    {
        if (assignment.ProgressPercent >= 100)
        {
            assignment.ProgressPercent = 100;
            assignment.Status = LearningAssignmentStatus.Completed;
            assignment.CompletedAt ??= DateTime.Now;
            assignment.StartedAt ??= assignment.CompletedAt;
            return;
        }

        assignment.CompletedAt = null;

        if (assignment.ProgressPercent > 0)
        {
            assignment.Status = LearningAssignmentStatus.InProgress;
            assignment.StartedAt ??= DateTime.Now;
            return;
        }

        assignment.Status = LearningAssignmentStatus.Assigned;
    }

    private async Task<LearningOperationResult> AccessDeniedAsync(string details, string message)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);

        return LearningOperationResult.Fail(message);
    }
}
