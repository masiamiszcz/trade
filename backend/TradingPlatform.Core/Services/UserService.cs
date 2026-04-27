using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountService _accountService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _hasher = new();

    public UserService(
        IUserRepository userRepository,
        IAccountService accountService,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<RegisterRequest> registerValidator,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _accountService = accountService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _registerValidator = registerValidator;
        _mapper = mapper;
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest registerRequest, CancellationToken cancellationToken = default)
    {
        // Validate input
        var validationResult = await _registerValidator.ValidateAsync(registerRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException(errorMessage);
        }

        // Check if username already exists
        var existingByUserName = await _userRepository.GetByUserNameAsync(registerRequest.UserName, cancellationToken);
        if (existingByUserName is not null)
            throw new InvalidOperationException("Username is already taken.");

        // Check if email already exists
        var existingByEmail = await _userRepository.GetByEmailAsync(registerRequest.Email, cancellationToken);
        if (existingByEmail is not null)
            throw new InvalidOperationException("Email is already registered.");

        // Create new user
        var user = new User(
            Id: Guid.NewGuid(),
            UserName: registerRequest.UserName.Trim(),
            Email: registerRequest.Email.Trim(),
            FirstName: registerRequest.FirstName.Trim(),
            LastName: registerRequest.LastName.Trim(),
            Role: UserRole.User,
            EmailConfirmed: false,
            TwoFactorEnabled: false,
            TwoFactorSecret: null,
            BackupCodes: null,
            Status: UserStatus.Active,
            BaseCurrency: registerRequest.BaseCurrency.ToUpper(),
            CreatedAtUtc: DateTimeOffset.UtcNow,

            // 🔥 NEW FIELDS
            BlockedUntilUtc: null,
            BlockReason: null,
            DeletedAtUtc: null
        );

        // Hash password and store user
        var passwordHash = _hasher.HashPassword(user, registerRequest.Password);
        await _userRepository.AddAsync(user, passwordHash, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Create main account for user
        await _accountService.CreateMainAccountAsync(
            user.Id, 
            user.BaseCurrency, 
            initialBalance: 10000, 
            cancellationToken);

        // Return user DTO
        return _mapper.Map<UserDto>(user);
    }

    public async Task<string> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(loginRequest.UserNameOrEmail) || string.IsNullOrWhiteSpace(loginRequest.Password))
            throw new ArgumentException("Username/Email and Password are required.");

        // Get user and password hash from repository
        var (user, hashedPassword) = await _userRepository.GetByUserNameOrEmailWithPasswordHashAsync(
            loginRequest.UserNameOrEmail, 
            cancellationToken);

        // Check if user exists and has password
        if (user is null || string.IsNullOrWhiteSpace(hashedPassword))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // Check if user is active
        if (user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("User is not active.");

        // Verify password
        var verifyResult = _hasher.VerifyHashedPassword(user, hashedPassword, loginRequest.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");

        // Generate and return JWT token
        var token = _jwtTokenGenerator.GenerateToken(user);
        return token;
    }
}