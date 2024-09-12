using apiforchat.Models;

namespace apiforchat.services
{
    public interface IJwtTokenService
    {
        //  string GenerateToken(User user);
        //  string GenerateRefreshToken();
        //  bool ValidateRefreshToken(string refreshToken)

        string GenerateToken(User user, double tokenExpirationInSeconds);
        string GenerateRefreshToken(User user, double refreshTokenExpirationInSeconds);

        bool ValidateToken(string token, bool isRefreshToken = false);
        bool ValidateRefreshToken(string refreshToken);
    }
}