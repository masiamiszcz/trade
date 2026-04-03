using Microsoft.AspNetCore.Identity;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly PasswordHasher<User> _hasher = new();

    public UserService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<User> RegisterAsync(RegisterRequest registerRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerRequest.UserName) || string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
        {
            throw new ArgumentException("UserName, Email and Password are required.");
        }

        var existingByUserName = await _userRepository.GetByUserNameAsync(registerRequest.UserName, cancellationToken);
        if (existingByUserName is not null)
        {
            throw new InvalidOperationException("UserName already exists.");
        }

        var existingByEmail = await _userRepository.GetByEmailAsync(registerRequest.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User(
            Guid.NewGuid(),
            registerRequest.UserName.Trim(),
            registerRequest.Email.Trim(),
            registerRequest.FirstName.Trim(),
            registerRequest.LastName.Trim(),
            UserRole.User,
            EmailConfirmed: false,
            TwoFactorEnabled: false,
            UserStatus.Active,
            DateTimeOffset.UtcNow);

        var passwordHash = _hasher.HashPassword(user, registerRequest.Password);

        await _userRepository.AddAsync(user, passwordHash, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<string> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.UserNameOrEmail) || string.IsNullOrWhiteSpace(loginRequest.Password))
        {
            throw new ArgumentException("UserName/Email and Password are required.");
        }

        var (user, hashedPassword) = await _userRepository.GetByUserNameOrEmailWithPasswordHashAsync(loginRequest.UserNameOrEmail, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new UnauthorizedAccessException("User is not active.");
        }

        var verify = _hasher.VerifyHashedPassword(user, hashedPassword, loginRequest.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return token;
    }
}
