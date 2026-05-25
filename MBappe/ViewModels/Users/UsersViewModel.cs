using MBappe.Common;
using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Users;

public partial class UsersViewModel : ViewModelBase
{
    private readonly UserManagementService _userManagementService;

    [ObservableProperty]
    private ObservableCollection<UserRowViewModel> users = [];

    [ObservableProperty]
    private UserRowViewModel? selectedUser;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string createFormMessage = string.Empty;

    [ObservableProperty]
    private string editFormMessage = string.Empty;

    [ObservableProperty]
    private string editFullName = string.Empty;

    [ObservableProperty]
    private string editEmail = string.Empty;

    [ObservableProperty]
    private string newLogin = string.Empty;

    [ObservableProperty]
    private string newEmail = string.Empty;

    [ObservableProperty]
    private string newFullName = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string newConfirmPassword = string.Empty;

    [ObservableProperty]
    private RoleOption selectedRole = RoleOption.All[0];

    [ObservableProperty]
    private RoleOption selectedEditRole = RoleOption.All[0];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCreateFormVisible;

    [ObservableProperty]
    private bool isEditFormVisible;

    public IReadOnlyList<RoleOption> RoleOptions { get; } = RoleOption.All;

    public bool HasSelectedUser => SelectedUser is not null;

    public bool CanChangeRoles => _userManagementService.CanChangeUserRoles();

    public bool HasCreateFormMessage => !string.IsNullOrWhiteSpace(CreateFormMessage);

    public bool HasEditFormMessage => !string.IsNullOrWhiteSpace(EditFormMessage);

    public string SelectedUserCaption => SelectedUser?.Login ?? "Выберите пользователя";

    public UsersViewModel(UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        var selectedUserId = SelectedUser?.User.Id;
        var result = await _userManagementService.GetAllUsersAsync();

        IsBusy = false;
        StatusMessage = result.Message;

        if (!result.Success || result.Users is null)
            return;

        Users = new ObservableCollection<UserRowViewModel>(
            result.Users
                .OrderBy(user => user.Role)
                .ThenBy(user => user.FullName)
                .Select(user => new UserRowViewModel(user)));

        SelectedUser = selectedUserId is null
            ? null
            : Users.FirstOrDefault(user => user.User.Id == selectedUserId.Value);
    }

    [RelayCommand]
    private void OpenCreateForm()
    {
        ClearCreateForm();
        IsCreateFormVisible = true;
        IsEditFormVisible = false;
        StatusMessage = string.Empty;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
    }

    [RelayCommand]
    private void OpenEditForm()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Выберите пользователя";
            return;
        }

        EditFullName = SelectedUser.FullName;
        EditEmail = SelectedUser.Email;
        SelectedEditRole = RoleOptions.First(option => option.Role == SelectedUser.User.Role);
        IsEditFormVisible = true;
        IsCreateFormVisible = false;
        StatusMessage = string.Empty;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
    }

    [RelayCommand]
    private void CloseUserForm()
    {
        IsCreateFormVisible = false;
        IsEditFormVisible = false;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveSelectedAsync()
    {
        if (SelectedUser is null)
        {
            EditFormMessage = "Выберите пользователя";
            return;
        }

        EditFormMessage = string.Empty;
        var selectedUserId = SelectedUser.User.Id;
        var roleWasChanged = CanChangeRoles && SelectedEditRole.Role != SelectedUser.User.Role;
        var request = new UpdateUserRequest
        {
            UserId = selectedUserId,
            FullName = EditFullName,
            Email = EditEmail
        };

        var result = await _userManagementService.UpdateUserAsync(request);

        if (!result.Success)
        {
            EditFormMessage = result.Message;
            return;
        }

        if (roleWasChanged)
        {
            var roleResult = await _userManagementService.ChangeUserRoleAsync(selectedUserId, SelectedEditRole.Role);

            if (!roleResult.Success)
            {
                EditFormMessage = roleResult.Message;
                return;
            }

            result = roleResult;
        }

        await RefreshAsync();
        SelectedUser = Users.FirstOrDefault(user => user.User.Id == selectedUserId);
        IsEditFormVisible = false;
        OnPropertyChanged(nameof(CanChangeRoles));
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;
        CreateFormMessage = string.Empty;

        var request = new CreateUserRequest
        {
            Login = NewLogin,
            Email = NewEmail,
            FullName = NewFullName,
            Password = NewPassword,
            ConfirmPassword = NewConfirmPassword,
            Role = SelectedRole.Role
        };

        var result = await _userManagementService.CreateUserAsync(request);

        IsBusy = false;

        if (!result.Success)
        {
            CreateFormMessage = result.Message;
            return;
        }

        ClearCreateForm();
        await RefreshAsync();
        IsCreateFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task BlockSelectedAsync()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Выберите пользователя";
            return;
        }

        var result = await _userManagementService.BlockUserAsync(SelectedUser.User.Id);
        StatusMessage = result.Message;

        if (result.Success)
        {
            await RefreshAsync();
            StatusMessage = result.Message;
        }
    }

    [RelayCommand]
    private async Task UnblockSelectedAsync()
    {
        if (SelectedUser is null)
        {
            StatusMessage = "Выберите пользователя";
            return;
        }

        var result = await _userManagementService.UnblockUserAsync(SelectedUser.User.Id);
        StatusMessage = result.Message;

        if (result.Success)
        {
            await RefreshAsync();
            StatusMessage = result.Message;
        }
    }

    private void ClearCreateForm()
    {
        NewLogin = string.Empty;
        NewEmail = string.Empty;
        NewFullName = string.Empty;
        NewPassword = string.Empty;
        NewConfirmPassword = string.Empty;
        SelectedRole = RoleOption.All[0];
    }

    partial void OnCreateFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasCreateFormMessage));
    }

    partial void OnEditFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasEditFormMessage));
    }

    partial void OnSelectedUserChanged(UserRowViewModel? value)
    {
        EditFullName = value?.FullName ?? string.Empty;
        EditEmail = value?.Email ?? string.Empty;
        SelectedEditRole = value is null
            ? RoleOption.All[0]
            : RoleOptions.First(option => option.Role == value.User.Role);
        OnPropertyChanged(nameof(HasSelectedUser));
        OnPropertyChanged(nameof(SelectedUserCaption));
    }
}

public sealed class UserRowViewModel : ViewModelBase
{
    public AppUser User { get; }

    public string FullName => User.FullName;

    public string Login => User.Login;

    public string Email => User.Email;

    public string RoleTitle => DisplayNames.ForRole(User.Role);

    public string StatusTitle => User.IsActive ? "Активен" : "Заблокирован";

    public string CreatedAtText => User.CreatedAt.ToString("dd.MM.yyyy");

    public UserRowViewModel(AppUser user)
    {
        User = user;
    }
}

public sealed class RoleOption
{
    public static IReadOnlyList<RoleOption> All { get; } =
    [
        new RoleOption(UserRole.Employee, "Сотрудник"),
        new RoleOption(UserRole.Manager, "Руководитель"),
        new RoleOption(UserRole.HrSpecialist, "HR-специалист"),
        new RoleOption(UserRole.Administrator, "Администратор")
    ];

    public UserRole Role { get; }

    public string Title { get; }

    private RoleOption(UserRole role, string title)
    {
        Role = role;
        Title = title;
    }
}
