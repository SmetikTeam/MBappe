using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class LearningServiceTests
{
    [Test]
    public async Task CreateCourseAsync_AsHrSpecialist_CreatesDraftCourseAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.LearningService.CreateCourseAsync(new CreateLearningCourseRequest
        {
            Title = " Security basics ",
            Description = " Required security onboarding ",
            Format = LearningFormat.Mixed,
            Provider = " Internal academy ",
            DurationHours = 8
        });

        var courses = await fixture.Services.LearningRepository.GetAllCoursesAsync();
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.LearningCourseCreated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Course, Is.Not.Null);
            Assert.That(courses, Has.One.SameAs(result.Course));
            Assert.That(result.Course!.Title, Is.EqualTo("Security basics"));
            Assert.That(result.Course.Description, Is.EqualTo("Required security onboarding"));
            Assert.That(result.Course.Provider, Is.EqualTo("Internal academy"));
            Assert.That(result.Course.Format, Is.EqualTo(LearningFormat.Mixed));
            Assert.That(result.Course.Status, Is.EqualTo(LearningCourseStatus.Draft));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details != null
                && entry.Details.Contains("Security basics")));
        });
    }

    [Test]
    public async Task CreateCourseAsync_AsEmployee_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.LearningService.CreateCourseAsync(new CreateLearningCourseRequest
        {
            Title = "Employee course",
            Description = "Employee course",
            Provider = "Internal",
            DurationHours = 2
        });

        var courses = await fixture.Services.LearningRepository.GetAllCoursesAsync();
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для создания курса"));
            Assert.That(courses, Is.Empty);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка создать курс обучения"));
        });
    }

    [Test]
    public async Task UpdateCourseAsync_AsAdministrator_UpdatesFieldsAndActivatesCourse()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Initial course", LearningCourseStatus.Draft);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.LearningService.UpdateCourseAsync(new UpdateLearningCourseRequest
        {
            CourseId = course.Id,
            Title = " Updated course ",
            Description = " Updated description ",
            Format = LearningFormat.Offline,
            Provider = " Updated provider ",
            DurationHours = 12,
            Status = LearningCourseStatus.Active
        });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.LearningCourseUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Course, Is.SameAs(course));
            Assert.That(course.Title, Is.EqualTo("Updated course"));
            Assert.That(course.Description, Is.EqualTo("Updated description"));
            Assert.That(course.Format, Is.EqualTo(LearningFormat.Offline));
            Assert.That(course.Provider, Is.EqualTo("Updated provider"));
            Assert.That(course.DurationHours, Is.EqualTo(12));
            Assert.That(course.Status, Is.EqualTo(LearningCourseStatus.Active));
            Assert.That(course.UpdatedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("Active")));
        });
    }

    [Test]
    public async Task AssignCourseAsync_AsHrSpecialist_AssignsActiveCourseAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.LearningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = course.Id,
            EmployeeId = fixture.EmployeeProfile.Id,
            DueDate = DateTime.Today.AddDays(14)
        });

        var assignment = await fixture.Services.LearningRepository.GetAssignmentAsync(
            course.Id,
            fixture.EmployeeProfile.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.LearningAssigned);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Assignment, Is.SameAs(assignment));
            Assert.That(assignment, Is.Not.Null);
            Assert.That(assignment!.AssignedByUserId, Is.EqualTo(fixture.HrUser.Id));
            Assert.That(assignment.Status, Is.EqualTo(LearningAssignmentStatus.Assigned));
            Assert.That(assignment.DueDate, Is.EqualTo(DateTime.Today.AddDays(14)));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details != null
                && entry.Details.Contains("Active course")));
        });
    }

    [Test]
    public async Task AssignCourseAsync_WhenCourseIsNotActive_ReturnsFailure()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Draft course", LearningCourseStatus.Draft);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.LearningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = course.Id,
            EmployeeId = fixture.EmployeeProfile.Id,
            DueDate = DateTime.Today.AddDays(14)
        });

        var assignments = await fixture.Services.LearningRepository.GetAllAssignmentsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Назначать можно только активный курс"));
            Assert.That(assignments, Is.Empty);
        });
    }

    [Test]
    public async Task AssignCourseAsync_WhenActiveAssignmentAlreadyExists_ReturnsFailure()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        await AddAssignmentAsync(fixture, course, fixture.EmployeeProfile, LearningAssignmentStatus.Assigned);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.LearningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = course.Id,
            EmployeeId = fixture.EmployeeProfile.Id,
            DueDate = DateTime.Today.AddDays(14)
        });

        var assignments = await fixture.Services.LearningRepository.GetAssignmentsByEmployeeIdAsync(
            fixture.EmployeeProfile.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Этот курс уже назначен сотруднику"));
            Assert.That(assignments, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GetVisibleAssignmentsAsync_ReturnsRoleScopedAssignments()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        var managerAssignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.ManagerProfile,
            LearningAssignmentStatus.Assigned);
        var employeeAssignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.EmployeeProfile,
            LearningAssignmentStatus.InProgress);
        var otherAssignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.OtherEmployeeProfile,
            LearningAssignmentStatus.Assigned);

        fixture.Services.SessionService.StartSession(fixture.AdminUser);
        var adminResult = await fixture.Services.LearningService.GetVisibleAssignmentsAsync();

        fixture.Services.SessionService.StartSession(fixture.ManagerUser);
        var managerResult = await fixture.Services.LearningService.GetVisibleAssignmentsAsync();

        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);
        var employeeResult = await fixture.Services.LearningService.GetVisibleAssignmentsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(adminResult.Success, Is.True);
            Assert.That(adminResult.Assignments, Is.EquivalentTo(new[]
            {
                managerAssignment,
                employeeAssignment,
                otherAssignment
            }));

            Assert.That(managerResult.Success, Is.True);
            Assert.That(managerResult.Assignments, Is.EquivalentTo(new[] { employeeAssignment }));

            Assert.That(employeeResult.Success, Is.True);
            Assert.That(employeeResult.Assignments, Is.EquivalentTo(new[] { employeeAssignment }));
        });
    }

    [Test]
    public async Task UpdateAssignmentProgressAsync_AsEmployeeForOwnAssignment_UpdatesStatusScoreAndDates()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        var assignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.EmployeeProfile,
            LearningAssignmentStatus.Assigned);
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var inProgressResult = await fixture.Services.LearningService.UpdateAssignmentProgressAsync(
            new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = assignment.Id,
                ProgressPercent = 50,
                Score = 80
            });
        var startedAt = assignment.StartedAt;

        var completedResult = await fixture.Services.LearningService.UpdateAssignmentProgressAsync(
            new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = assignment.Id,
                ProgressPercent = 100,
                Score = 95
            });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.LearningProgressUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(inProgressResult.Success, Is.True);
            Assert.That(startedAt, Is.Not.Null);
            Assert.That(completedResult.Success, Is.True);
            Assert.That(assignment.Status, Is.EqualTo(LearningAssignmentStatus.Completed));
            Assert.That(assignment.ProgressPercent, Is.EqualTo(100));
            Assert.That(assignment.Score, Is.EqualTo(95));
            Assert.That(assignment.CompletedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.Count.EqualTo(2));
            Assert.That(auditEntries, Has.Some.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details != null
                && entry.Details.Contains("прогресс: 100%")));
        });
    }

    [Test]
    public async Task UpdateAssignmentProgressAsync_AsManagerForDirectReport_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        var assignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.EmployeeProfile,
            LearningAssignmentStatus.Assigned);
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.LearningService.UpdateAssignmentProgressAsync(
            new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = assignment.Id,
                ProgressPercent = 50,
                Score = 80
            });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для обновления прогресса"));
            Assert.That(assignment.Status, Is.EqualTo(LearningAssignmentStatus.Assigned));
            Assert.That(assignment.ProgressPercent, Is.EqualTo(0));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details == "Попытка обновить прогресс обучения без прав"));
        });
    }

    [Test]
    public async Task CancelAssignmentAsync_AsHrSpecialist_CancelsAssignmentAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        var assignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.EmployeeProfile,
            LearningAssignmentStatus.InProgress,
            progressPercent: 40);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.LearningService.CancelAssignmentAsync(assignment.Id);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.LearningAssignmentCancelled);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Assignment, Is.SameAs(assignment));
            Assert.That(assignment.Status, Is.EqualTo(LearningAssignmentStatus.Cancelled));
            Assert.That(assignment.UpdatedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "hr"));
        });
    }

    [Test]
    public async Task CancelAssignmentAsync_AsEmployee_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        var course = await AddCourseAsync(fixture, "Active course", LearningCourseStatus.Active);
        var assignment = await AddAssignmentAsync(
            fixture,
            course,
            fixture.EmployeeProfile,
            LearningAssignmentStatus.Assigned);
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.LearningService.CancelAssignmentAsync(assignment.Id);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для отмены обучения"));
            Assert.That(assignment.Status, Is.EqualTo(LearningAssignmentStatus.Assigned));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка отменить обучение без прав"));
        });
    }

    private static async Task<LearningFixture> CreateFixtureAsync()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var hr = await GetSeedUserAsync(services, "hr");
        var manager = await GetSeedUserAsync(services, "manager");
        var employee = await GetSeedUserAsync(services, "employee");

        var managerProfile = new EmployeeProfile
        {
            UserId = manager.Id,
            PersonnelNumber = "M-001",
            FullName = "Анна Смирнова",
            Position = "Engineering manager",
            Department = "Engineering",
            Status = EmployeeStatus.Active
        };
        var employeeProfile = new EmployeeProfile
        {
            UserId = employee.Id,
            PersonnelNumber = "E-001",
            FullName = "Иван Петров",
            Position = "Developer",
            Department = "Engineering",
            ManagerEmployeeId = managerProfile.Id,
            Status = EmployeeStatus.Active
        };
        var otherEmployeeProfile = new EmployeeProfile
        {
            UserId = Guid.NewGuid(),
            PersonnelNumber = "O-001",
            FullName = "Петр Другой",
            Position = "Analyst",
            Department = "Sales",
            Status = EmployeeStatus.Active
        };

        await services.EmployeeRepository.AddAsync(managerProfile);
        await services.EmployeeRepository.AddAsync(employeeProfile);
        await services.EmployeeRepository.AddAsync(otherEmployeeProfile);

        return new LearningFixture(
            services,
            admin,
            hr,
            manager,
            employee,
            managerProfile,
            employeeProfile,
            otherEmployeeProfile);
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private static async Task<LearningCourse> AddCourseAsync(
        LearningFixture fixture,
        string title,
        LearningCourseStatus status)
    {
        var course = new LearningCourse
        {
            Title = title,
            Description = title,
            Provider = "Internal",
            DurationHours = 4,
            Format = LearningFormat.Online,
            Status = status
        };

        await fixture.Services.LearningRepository.AddCourseAsync(course);
        return course;
    }

    private static async Task<LearningAssignment> AddAssignmentAsync(
        LearningFixture fixture,
        LearningCourse course,
        EmployeeProfile employee,
        LearningAssignmentStatus status,
        double progressPercent = 0)
    {
        var assignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employee.Id,
            AssignedByUserId = fixture.AdminUser.Id,
            AssignedAt = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            ProgressPercent = progressPercent,
            Status = status
        };

        await fixture.Services.LearningRepository.AddAssignmentAsync(assignment);
        return assignment;
    }

    private sealed record LearningFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser HrUser,
        AppUser ManagerUser,
        AppUser EmployeeUser,
        EmployeeProfile ManagerProfile,
        EmployeeProfile EmployeeProfile,
        EmployeeProfile OtherEmployeeProfile);
}
