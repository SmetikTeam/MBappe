using MBappe.Models;
using MBappe.Repositories;

namespace MBappe.Tests.Repositories;

[TestFixture]
public class InMemoryRepositoryTests
{
    [Test]
    public async Task EmployeeRepository_GetAllAndPersonnelNumberQueriesUseExpectedOrderingAndComparison()
    {
        var repository = new InMemoryEmployeeRepository();
        var second = new EmployeeProfile
        {
            FullName = "B Employee",
            PersonnelNumber = "E-002"
        };
        var first = new EmployeeProfile
        {
            FullName = "A Employee",
            PersonnelNumber = "E-001"
        };

        await repository.AddAsync(second);
        await repository.AddAsync(first);

        var allEmployees = await repository.GetAllAsync();
        var foundByNumber = await repository.GetByPersonnelNumberAsync("e-001");

        Assert.Multiple(() =>
        {
            Assert.That(allEmployees.Select(employee => employee.FullName), Is.EqualTo(new[]
            {
                "A Employee",
                "B Employee"
            }));
            Assert.That(foundByNumber, Is.SameAs(first));
        });
    }

    [Test]
    public async Task KpiRepository_GetByPeriod_ReturnsOnlyIntersectingKpisInCreatedAtDescendingOrder()
    {
        var repository = new InMemoryKpiRepository();
        var oldKpi = CreateKpi("Old", new DateTime(2025, 12, 1), new DateTime(2025, 12, 31), new DateTime(2026, 1, 1));
        var firstIntersecting = CreateKpi("First", new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), new DateTime(2026, 1, 2));
        var secondIntersecting = CreateKpi("Second", new DateTime(2026, 1, 20), new DateTime(2026, 2, 5), new DateTime(2026, 1, 3));
        var futureKpi = CreateKpi("Future", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), new DateTime(2026, 1, 4));

        foreach (var kpi in new[] { oldKpi, firstIntersecting, secondIntersecting, futureKpi })
            await repository.AddAsync(kpi);

        var periodKpis = await repository.GetByPeriodAsync(new DateTime(2026, 1, 5), new DateTime(2026, 1, 31));

        Assert.That(periodKpis.Select(kpi => kpi.Title), Is.EqualTo(new[]
        {
            "Second",
            "First"
        }));
    }

    [Test]
    public async Task LearningRepository_AssignmentQueriesFilterByCourseAndEmployeeInAssignedAtDescendingOrder()
    {
        var repository = new InMemoryLearningRepository();
        var course = new LearningCourse { Title = "Active course" };
        var employeeId = Guid.NewGuid();
        var anotherEmployeeId = Guid.NewGuid();
        var olderAssignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employeeId,
            Status = LearningAssignmentStatus.Completed,
            AssignedAt = new DateTime(2026, 1, 1)
        };
        var newerAssignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employeeId,
            Status = LearningAssignmentStatus.Assigned,
            AssignedAt = new DateTime(2026, 1, 2)
        };
        var anotherAssignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = anotherEmployeeId,
            Status = LearningAssignmentStatus.Assigned,
            AssignedAt = new DateTime(2026, 1, 3)
        };

        await repository.AddCourseAsync(course);
        await repository.AddAssignmentAsync(olderAssignment);
        await repository.AddAssignmentAsync(newerAssignment);
        await repository.AddAssignmentAsync(anotherAssignment);

        var courseAssignments = await repository.GetAssignmentsByCourseIdAsync(course.Id);
        var employeeAssignments = await repository.GetAssignmentsByEmployeeIdAsync(employeeId);

        Assert.Multiple(() =>
        {
            Assert.That(courseAssignments.Select(assignment => assignment.Id), Is.EqualTo(new[]
            {
                anotherAssignment.Id,
                newerAssignment.Id,
                olderAssignment.Id
            }));
            Assert.That(employeeAssignments.Select(assignment => assignment.Id), Is.EqualTo(new[]
            {
                newerAssignment.Id,
                olderAssignment.Id
            }));
        });
    }

    [Test]
    public async Task MotivationBonusRepository_FindExistingAsync_RequiresSamePeriodProgramEmployeeAndIgnoresCancelled()
    {
        var repository = new InMemoryMotivationBonusRepository();
        var employeeId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var cancelled = new MotivationBonus
        {
            EmployeeId = employeeId,
            ProgramId = programId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = MotivationBonusStatus.Cancelled
        };
        var active = new MotivationBonus
        {
            EmployeeId = employeeId,
            ProgramId = programId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = MotivationBonusStatus.Approved
        };

        await repository.AddAsync(cancelled);
        var existingBeforeActive = await repository.FindExistingAsync(employeeId, programId, periodStart, periodEnd);
        await repository.AddAsync(active);
        var existingAfterActive = await repository.FindExistingAsync(employeeId, programId, periodStart, periodEnd);
        var existingForAnotherPeriod = await repository.FindExistingAsync(
            employeeId,
            programId,
            periodStart.AddMonths(-1),
            periodEnd.AddMonths(-1));

        Assert.Multiple(() =>
        {
            Assert.That(existingBeforeActive, Is.Null);
            Assert.That(existingAfterActive, Is.SameAs(active));
            Assert.That(existingForAnotherPeriod, Is.Null);
        });
    }

    [Test]
    public async Task MotivationProgramRepository_GetAll_ReturnsActiveProgramsBeforeInactiveThenByTitle()
    {
        var repository = new InMemoryMotivationProgramRepository();
        var inactiveA = new MotivationProgram { Title = "A inactive", IsActive = false };
        var activeB = new MotivationProgram { Title = "B active", IsActive = true };
        var activeA = new MotivationProgram { Title = "A active", IsActive = true };

        await repository.AddAsync(inactiveA);
        await repository.AddAsync(activeB);
        await repository.AddAsync(activeA);

        var programs = (await repository.GetAllAsync()).ToList();
        var firstInactiveIndex = programs
            .Select((program, index) => new { program, index })
            .First(item => !item.program.IsActive)
            .index;

        Assert.Multiple(() =>
        {
            Assert.That(programs.Take(firstInactiveIndex), Is.All.Matches<MotivationProgram>(program => program.IsActive));
            Assert.That(programs.Skip(firstInactiveIndex), Is.All.Matches<MotivationProgram>(program => !program.IsActive));
            Assert.That(programs.IndexOf(activeA), Is.LessThan(programs.IndexOf(activeB)));
            Assert.That(programs.Last(), Is.SameAs(inactiveA));
        });
    }

    private static KpiItem CreateKpi(
        string title,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime createdAt)
    {
        return new KpiItem
        {
            Title = title,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = createdAt
        };
    }
}
