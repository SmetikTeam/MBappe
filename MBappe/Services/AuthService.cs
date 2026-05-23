using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Threading.Tasks;

namespace MBappe.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public AuthService(
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

    public async Task<AuthResult> LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserLoginFailed,
                false,
                "Неудачная попытка входа",
                "Логин не был введен");

            return AuthResult.Fail("Введите логин");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserLoginFailed,
                false,
                "Неудачная попытка входа",
                "Пароль не был введен",
                login: login.Trim());

            return AuthResult.Fail("Введите пароль");
        }

        var normalizedLogin = login.Trim();

        var user = await _userRepository.GetByLoginAsync(normalizedLogin);

        if (user is null)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserLoginFailed,
                false,
                "Неудачная попытка входа",
                "Пользователь с таким логином не найден",
                login: normalizedLogin);

            return AuthResult.Fail("Пользователь с таким логином не найден");
        }

        if (!user.IsActive)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserLoginFailed,
                false,
                "Неудачная попытка входа",
                "Аккаунт пользователя заблокирован",
                user: user);

            return AuthResult.Fail("Аккаунт заблокирован");
        }

        var passwordIsValid = _passwordHasher.VerifyPassword(
            password,
            user.PasswordSalt,
            user.PasswordHash);

        if (!passwordIsValid)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserLoginFailed,
                false,
                "Неудачная попытка входа",
                "Введен неверный пароль",
                user: user);

            return AuthResult.Fail("Неверный пароль");
        }

        user.LastLoginAt = DateTime.Now;
        await _userRepository.UpdateAsync(user);

        _sessionService.StartSession(user);

        await _auditLogService.LogAsync(
            AuditActionType.UserLoginSuccess,
            true,
            "Пользователь успешно вошел в систему",
            $"Роль пользователя: {user.Role}",
            user: user);

        return AuthResult.Ok(user, $"Добро пожаловать, {user.FullName}");
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var validationError = ValidateRegisterRequest(request);

        if (validationError is not null)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserRegistrationFailed,
                false,
                "Неудачная попытка регистрации",
                validationError,
                login: request.Login?.Trim());

            return AuthResult.Fail(validationError);
        }

        var normalizedLogin = request.Login.Trim();
        var normalizedEmail = request.Email.Trim();

        var loginAlreadyExists = await _userRepository.GetByLoginAsync(normalizedLogin);

        if (loginAlreadyExists is not null)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserRegistrationFailed,
                false,
                "Неудачная попытка регистрации",
                "Пользователь с таким логином уже существует",
                login: normalizedLogin);

            return AuthResult.Fail("Пользователь с таким логином уже существует");
        }

        var emailAlreadyExists = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (emailAlreadyExists is not null)
        {
            await _auditLogService.LogAsync(
                AuditActionType.UserRegistrationFailed,
                false,
                "Неудачная попытка регистрации",
                "Пользователь с такой почтой уже существует",
                login: normalizedLogin);

            return AuthResult.Fail("Пользователь с такой почтой уже существует");
        }

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
            AuditActionType.UserRegistrationSuccess,
            true,
            "Пользователь успешно зарегистрирован",
            $"Создан пользователь с ролью {user.Role}",
            user: user);

        return AuthResult.Ok(user, "Пользователь успешно зарегистрирован");
    }

    public async Task LogoutAsync()
    {
        await _auditLogService.LogAsync(
            AuditActionType.UserLogout,
            true,
            "Пользователь вышел из системы");

        _sessionService.EndSession();
    }

    private static string? ValidateRegisterRequest(RegisterRequest request)
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