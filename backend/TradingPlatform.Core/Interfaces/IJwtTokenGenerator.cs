using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
