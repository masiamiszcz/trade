using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterRequest registerRequest, CancellationToken cancellationToken = default);
    Task<string> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);
}
