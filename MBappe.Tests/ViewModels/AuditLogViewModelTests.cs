using MBappe.Models;
using MBappe.Tests.TestInfrastructure;
using MBappe.ViewModels.Audit;

namespace MBappe.Tests.ViewModels;

[TestFixture]
public class AuditLogViewModelTests
{
    [Test]
    public async Task RefreshCommand_LoadsSummaryAndAppliesSearchAndResultFilter()
    {
        var services = TestServiceFactory.Create();
        await services.AuditLogRepository.AddAsync(new AuditLogEntry
        {
            CreatedAt = new DateTime(2026, 6, 2, 12, 30, 0),
            UserLogin = "admin",
            UserRole = UserRole.Administrator,
            ActionType = AuditActionType.AnalyticsReportGenerated,
            IsSuccess = true,
            Message = "Аналитический отчет сформирован",
            Details = "Период: 01.05.2026 - 01.06.2026"
        });
        await services.AuditLogRepository.AddAsync(new AuditLogEntry
        {
            CreatedAt = new DateTime(2026, 6, 1, 11, 15, 0),
            UserLogin = "employee",
            UserRole = UserRole.Employee,
            ActionType = AuditActionType.AccessDenied,
            IsSuccess = false,
            Message = "Доступ запрещен",
            Details = "Попытка открыть администрирование"
        });

        var viewModel = new AuditLogViewModel(services.AuditLogService);
        await WaitForRefreshAsync(viewModel, 2);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.TotalEntryCount, Is.EqualTo(2));
            Assert.That(viewModel.SuccessEntryCount, Is.EqualTo(1));
            Assert.That(viewModel.FailedEntryCount, Is.EqualTo(1));
            Assert.That(viewModel.LastEntryText, Is.EqualTo("02.06.2026 12:30"));
            Assert.That(viewModel.LastEntryActionText, Is.EqualTo("Формирование аналитического отчета"));
            Assert.That(viewModel.Entries, Has.Count.EqualTo(2));
            Assert.That(viewModel.HasEntries, Is.True);
            Assert.That(viewModel.HasNoEntries, Is.False);
        });

        viewModel.SearchText = "доступ";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries, Has.Count.EqualTo(1));
            Assert.That(viewModel.Entries.Single().ActionText, Is.EqualTo("Отказ в доступе"));
            Assert.That(viewModel.StatusMessage, Is.EqualTo("Показано: 1 из 2"));
        });

        viewModel.SelectedResultFilter = viewModel.ResultFilterOptions.Single(option => option.IsSuccess == true);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries, Is.Empty);
            Assert.That(viewModel.HasEntries, Is.False);
            Assert.That(viewModel.HasNoEntries, Is.True);
            Assert.That(viewModel.EmptyStateText, Is.EqualTo("По заданным фильтрам записи не найдены"));
        });

        viewModel.ClearFiltersCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries, Has.Count.EqualTo(2));
            Assert.That(viewModel.StatusMessage, Is.EqualTo("Показано: 2 из 2"));
        });
    }

    private static async Task WaitForRefreshAsync(AuditLogViewModel viewModel, int expectedEntries)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (!viewModel.IsBusy && viewModel.TotalEntryCount == expectedEntries)
                return;

            await Task.Delay(20);
        }

        Assert.Fail("Audit log refresh did not finish in time.");
    }
}
