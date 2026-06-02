using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Audit;

public partial class AuditLogViewModel : ViewModelBase
{
    private readonly AuditLogService _auditLogService;
    private IReadOnlyList<AuditLogEntry> allEntries = [];

    [ObservableProperty]
    private ObservableCollection<AuditLogRowViewModel> entries = [];

    [ObservableProperty]
    private ObservableCollection<AuditResultFilterOption> resultFilterOptions = [];

    [ObservableProperty]
    private AuditResultFilterOption? selectedResultFilter;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int totalEntryCount;

    [ObservableProperty]
    private int successEntryCount;

    [ObservableProperty]
    private int failedEntryCount;

    [ObservableProperty]
    private string lastEntryText = "-";

    [ObservableProperty]
    private string lastEntryActionText = "Нет данных";

    public bool HasEntries => Entries.Count > 0;

    public bool HasNoEntries => !HasEntries && !IsBusy;

    public string EmptyStateText => TotalEntryCount == 0
        ? "В журнале пока нет записей"
        : "По заданным фильтрам записи не найдены";

    public AuditLogViewModel(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        ResultFilterOptions = new ObservableCollection<AuditResultFilterOption>
        {
            new("Все результаты", null),
            new("Только успешные", true),
            new("Только ошибки", false)
        };
        SelectedResultFilter = ResultFilterOptions.First();

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            allEntries = await _auditLogService.GetAllAsync();

            allEntries = allEntries
                .OrderByDescending(entry => entry.CreatedAt)
                .ToList();

            UpdateSummary();
            ApplyFilters();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Не удалось загрузить журнал: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateEntryStateNotifications();
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedResultFilter = ResultFilterOptions.FirstOrDefault();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = SearchText.Trim();
        var resultFilter = SelectedResultFilter?.IsSuccess;

        var filteredEntries = allEntries.Where(entry =>
        {
            if (resultFilter.HasValue && entry.IsSuccess != resultFilter.Value)
                return false;

            return string.IsNullOrWhiteSpace(query) || MatchesSearch(entry, query);
        });

        Entries = new ObservableCollection<AuditLogRowViewModel>(
            filteredEntries.Select(entry => new AuditLogRowViewModel(entry)));

        StatusMessage = $"Показано: {Entries.Count} из {TotalEntryCount}";
        UpdateEntryStateNotifications();
    }

    private static bool MatchesSearch(AuditLogEntry entry, string query)
    {
        return Contains(entry.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"), query)
            || Contains(entry.UserLogin, query)
            || Contains(entry.UserRole.HasValue ? DisplayNames.ForRole(entry.UserRole.Value) : null, query)
            || Contains(DisplayNames.ForAuditAction(entry.ActionType), query)
            || Contains(entry.ActionType.ToString(), query)
            || Contains(entry.Message, query)
            || Contains(entry.Details, query);
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void UpdateSummary()
    {
        TotalEntryCount = allEntries.Count;
        SuccessEntryCount = allEntries.Count(entry => entry.IsSuccess);
        FailedEntryCount = TotalEntryCount - SuccessEntryCount;

        var lastEntry = allEntries.FirstOrDefault();
        LastEntryText = lastEntry?.CreatedAt.ToString("dd.MM.yyyy HH:mm") ?? "-";
        LastEntryActionText = lastEntry is null
            ? "Нет данных"
            : DisplayNames.ForAuditAction(lastEntry.ActionType);
    }

    private void UpdateEntryStateNotifications()
    {
        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(HasNoEntries));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedResultFilterChanged(AuditResultFilterOption? value)
    {
        ApplyFilters();
    }

    partial void OnIsBusyChanged(bool value)
    {
        UpdateEntryStateNotifications();
    }

    partial void OnTotalEntryCountChanged(int value)
    {
        OnPropertyChanged(nameof(EmptyStateText));
    }
}

public sealed class AuditLogRowViewModel
{
    public AuditLogEntry Entry { get; }

    public string CreatedAtText => Entry.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");

    public string UserText => Entry.UserLogin ?? "-";

    public string RoleText => Entry.UserRole is null ? "-" : DisplayNames.ForRole(Entry.UserRole.Value);

    public string ActionText => DisplayNames.ForAuditAction(Entry.ActionType);

    public string ResultText => Entry.IsSuccess ? "Успешно" : "Ошибка";

    public bool IsSuccess => Entry.IsSuccess;

    public bool IsFailure => !Entry.IsSuccess;

    public string Message => Entry.Message;

    public string Details => string.IsNullOrWhiteSpace(Entry.Details) ? "-" : Entry.Details;

    public AuditLogRowViewModel(AuditLogEntry entry)
    {
        Entry = entry;
    }
}

public sealed class AuditResultFilterOption
{
    public string Title { get; }

    public bool? IsSuccess { get; }

    public AuditResultFilterOption(string title, bool? isSuccess)
    {
        Title = title;
        IsSuccess = isSuccess;
    }

    public override string ToString()
    {
        return Title;
    }
}
