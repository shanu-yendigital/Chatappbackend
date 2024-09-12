using apiforchat.Models;

namespace apiforchat.services
{
    public class AuthService
    {
        private readonly JwtTokenService _jwtTokenService;

        // Constructor to initialize JwtTokenService
        public AuthService(JwtTokenService jwtTokenService)
        {
            _jwtTokenService = jwtTokenService;
        }

    }
}
