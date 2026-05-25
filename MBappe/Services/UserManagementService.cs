using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Threading.Tasks;

namespace MBappe.Services;

public class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public UserManagementService(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public async Task<UserOperationResult> GetAllUsersAsync()
    {
        if (!CanManageUsers())
        {
            await LogAccessDeniedAsync("Попытка получить список пользователей");
            return UserOperationResult.Fail("Недостаточно прав для просмотра списка пользователей");
        }

        var users = await _userRepository.GetAllAsync();

        await _auditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Получен список пользователей",
            $"Количество пользователей: {users.Count}");

        return UserOperationResult.Ok(users, "Список пользователей получен");
    }

    public async Task<UserOperationResult> CreateUserAsync(CreateUserRequest request)
    {
        if (!CanManageUsers())
        {
            await LogAccessDeniedAsync("Попытка создать пользователя");
            return UserOperationResult.Fail("Недостаточно прав для создания пользователя");
        }

        var validationError = ValidateCreateRequest(request);

        if (validationError is not null)
            return UserOperationResult.Fail(validationError);

        var normalizedLogin = request.Login.Trim();
        var normalizedEmail = request.Email.Trim();

        var userWithSameLogin = await _userRepository.GetByLoginAsync(normalizedLogin);

        if (userWithSameLogin is not null)
            return UserOperationResult.Fail("Пользователь с таким логином уже существует");

        var userWithSameEmail = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (userWithSameEmail is not null)
            return UserOperationResult.Fail("Пользователь с такой почтой уже существует");

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.HashPassword(request.Password, salt);

        var user = new AppUser
        {
            Login = normalizedLogin,
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordSalt = salt,
            PasswordHash = hash,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _userRepository.AddAsync(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserCreated,
            true,
            "Создан пользователь",
            $"Логин: {user.Login}, роль: {user.Role}",
            user: _sessionService.CurrentUser);

        return UserOperationResult.Ok(user, "Пользователь успешно создан");
    }

    public async Task<UserOperationResult> UpdateUserAsync(UpdateUserRequest request)
    {
        if (!CanManageUsers())
        {
            await LogAccessDeniedAsync("Попытка изменить данные пользователя");
            return UserOperationResult.Fail("Недостаточно прав для изменения пользователя");
        }

        if (request.UserId == Guid.Empty)
            return UserOperationResult.Fail("Не указан пользователь");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return UserOperationResult.Fail("Введите ФИО");

        if (string.IsNullOrWhiteSpace(request.Email))
            return UserOperationResult.Fail("Введите почту");

        if (!request.Email.Contains('@'))
            return UserOperationResult.Fail("Некорректная почта");

        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user is null)
            return UserOperationResult.Fail("Пользователь не найден");

        var normalizedEmail = request.Email.Trim();

        var userWithSameEmail = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (userWithSameEmail is not null && userWithSameEmail.Id != user.Id)
            return UserOperationResult.Fail("Почта уже используется другим пользователем");

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;

        await _userRepository.UpdateAsync(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserUpdated,
            true,
            "Изменены данные пользователя",
            $"Изменен пользователь: {user.Login}",
            user: _sessionService.CurrentUser);

        return UserOperationResult.Ok(user, "Данные пользователя обновлены");
    }

    public async Task<UserOperationResult> ChangeUserRoleAsync(Guid userId, UserRole newRole)
    {
        if (!_sessionService.HasRole(UserRole.Administrator))
        {
            await LogAccessDeniedAsync("Попытка изменить роль пользователя");
            return UserOperationResult.Fail("Только администратор может изменять роли пользователей");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
            return UserOperationResult.Fail("Пользователь не найден");

        var oldRole = user.Role;
        user.Role = newRole;

        await _userRepository.UpdateAsync(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserRoleChanged,
            true,
            "Изменена роль пользователя",
            $"Пользователь: {user.Login}, старая роль: {oldRole}, новая роль: {newRole}",
            user: _sessionService.CurrentUser);

        return UserOperationResult.Ok(user, "Роль пользователя изменена");
    }

    public bool CanChangeUserRoles()
    {
        return _sessionService.HasRole(UserRole.Administrator);
    }

    public async Task<UserOperationResult> BlockUserAsync(Guid userId)
    {
        if (!CanManageUsers())
        {
            await LogAccessDeniedAsync("Попытка заблокировать пользователя");
            return UserOperationResult.Fail("Недостаточно прав для блокировки пользователя");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
            return UserOperationResult.Fail("Пользователь не найден");

        if (user.Id == _sessionService.CurrentUser?.Id)
            return UserOperationResult.Fail("Нельзя заблокировать собственный аккаунт");

        if (!user.IsActive)
            return UserOperationResult.Fail("Пользователь уже заблокирован");

        user.IsActive = false;

        await _userRepository.UpdateAsync(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserBlocked,
            true,
            "Пользователь заблокирован",
            $"Заблокирован пользователь: {user.Login}",
            user: _sessionService.CurrentUser);

        return UserOperationResult.Ok(user, "Пользователь заблокирован");
    }

    public async Task<UserOperationResult> UnblockUserAsync(Guid userId)
    {
        if (!CanManageUsers())
        {
            await LogAccessDeniedAsync("Попытка разблокировать пользователя");
            return UserOperationResult.Fail("Недостаточно прав для разблокировки пользователя");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
            return UserOperationResult.Fail("Пользователь не найден");

        if (user.IsActive)
            return UserOperationResult.Fail("Пользователь уже активен");

        user.IsActive = true;

        await _userRepository.UpdateAsync(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserUnblocked,
            true,
            "Пользователь разблокирован",
            $"Разблокирован пользователь: {user.Login}",
            user: _sessionService.CurrentUser);

        return UserOperationResult.Ok(user, "Пользователь разблокирован");
    }

    private bool CanManageUsers()
    {
        return _sessionService.HasAnyRole(
            UserRole.Administrator,
            UserRole.HrSpecialist);
    }

    private async Task LogAccessDeniedAsync(string details)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);
    }

    private static string? ValidateCreateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
            return "Введите логин";

        if (request.Login.Trim().Length < 3)
            return "Логин должен содержать минимум 3 символа";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Введите почту";

        if (!request.Email.Contains('@'))
            return "Некорректная почта";

        if (string.IsNullOrWhiteSpace(request.FullName))
            return "Введите ФИО";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Введите пароль";

        if (request.Password.Length < 5)
            return "Пароль должен содержать минимум 5 символов";

        if (request.Password != request.ConfirmPassword)
            return "Пароли не совпадают";

        return null;
    }
}
