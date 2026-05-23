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

    public AuthService(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        SessionService sessionService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
    }

    public async Task<AuthResult> LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
            return AuthResult.Fail("Введите логин");

        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Введите пароль");

        var user = await _userRepository.GetByLoginAsync(login.Trim());

        if (user is null)
            return AuthResult.Fail("Пользователь с таким логином не найден");

        if (!user.IsActive)
            return AuthResult.Fail("Аккаунт заблокирован");

        var passwordIsValid = _passwordHasher.VerifyPassword(
            password,
            user.PasswordSalt,
            user.PasswordHash);

        if (!passwordIsValid)
            return AuthResult.Fail("Неверный пароль");

        user.LastLoginAt = DateTime.Now;
        await _userRepository.UpdateAsync(user);

        _sessionService.StartSession(user);

        return AuthResult.Ok(user, $"Добро пожаловать, {user.FullName}");
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var validationError = ValidateRegisterRequest(request);

        if (validationError is not null)
            return AuthResult.Fail(validationError);

        var loginAlreadyExists = await _userRepository.GetByLoginAsync(request.Login.Trim());

        if (loginAlreadyExists is not null)
            return AuthResult.Fail("Пользователь с таким логином уже существует");

        var emailAlreadyExists = await _userRepository.GetByEmailAsync(request.Email.Trim());

        if (emailAlreadyExists is not null)
            return AuthResult.Fail("Пользователь с такой почтой уже существует");

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.HashPassword(request.Password, salt);

        var user = new AppUser
        {
            Login = request.Login.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PasswordSalt = salt,
            PasswordHash = hash,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _userRepository.AddAsync(user);

        return AuthResult.Ok(user, "Пользователь успешно зарегистрирован");
    }

    public void Logout()
    {
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