using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Audit;

public partial class AuditLogViewModel : ViewModelBase
{
    private readonly AuditLogService _auditLogService;

    [ObservableProperty]
    private ObservableCollection<AuditLogRowViewModel> entries = [];

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public AuditLogViewModel(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        var entries = await _auditLogService.GetAllAsync();

        Entries = new ObservableCollection<AuditLogRowViewModel>(
            entries
                .OrderByDescending(entry => entry.CreatedAt)
                .Select(entry => new AuditLogRowViewModel(entry)));

        StatusMessage = $"Записей: {Entries.Count}";
        IsBusy = false;
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

    public string Message => Entry.Message;

    public string Details => string.IsNullOrWhiteSpace(Entry.Details) ? "-" : Entry.Details;

    public AuditLogRowViewModel(AuditLogEntry entry)
    {
        Entry = entry;
    }
}
