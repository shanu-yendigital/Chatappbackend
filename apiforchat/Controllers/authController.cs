using apiforchat.Models;
using apiforchat.services;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Net.WebRequestMethods;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;

namespace apiforchat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Private field to hold the MongoDBService instance
        private readonly MongoDBService _mongoDBService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration; 

       
        public AuthController(IConfiguration configuration, MongoDBService mongoDBService, IJwtTokenService jwtTokenService)
        {
            _mongoDBService = mongoDBService;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration ?? throw new Exception("Configuration is not properly initialized.");

        }

        // The [FromBody] attribute tells ASP.NET Core to bind the request body to the SignUpRequest model
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            Console.WriteLine("SignUp method called.");
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrEmpty(request.Email))
            {
                Console.WriteLine("SignUp failed: Missing fields.");
                return BadRequest("All fields are required.");
            }

            var existingUser = await _mongoDBService.GetUserByUsernameAsync(request.Username);

            if (existingUser != null)
            {
                
                return BadRequest("Username already exists.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create a new User object with the provided details and the hashed password
            var user = new User
            {
                Username = request.Username,
                PasswordHash = hashedPassword,
                Email = request.Email
            };

            // Save the new user to the database using the MongoDBService
            await _mongoDBService.CreateUserAsync(user);

            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (_configuration == null)
            {
                throw new Exception("Configuration is not properly initialized.");
            }
            // Check if the required fields are provided
            if (ModelState.IsValid)
            {
                // Authenticate the user using the provided username and password
                var user = await _mongoDBService.AuthenticateAsync(request.Username, request.Password);

                // If authentication fails, return an Unauthorized response
                if (user == null)
                {
                    return Unauthorized("Invalid username or password");
                }

                if (_configuration == null)
                {
                    throw new Exception("Configuration is not properly initialized.");
                }
                else
                {
                    Console.WriteLine("Configuration loaded successfully");
                }
                var tokenExpirationInSeconds = double.Parse(_configuration["Jwt:AccessTokenExpirationInSeconds"]);
                var refreshTokenExpirationInSeconds = double.Parse(_configuration["Jwt:RefreshTokenExpirationInSeconds"]);
         
               
                var accessToken = _jwtTokenService.GenerateToken(user, tokenExpirationInSeconds);
                var refreshToken = _jwtTokenService.GenerateRefreshToken(user, refreshTokenExpirationInSeconds);

                // Return the tokens to the client
                return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // Refresh the access token using the provided refresh token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Validate the refresh token
            if (_jwtTokenService.ValidateRefreshToken(request.RefreshToken))
            {
                var user = await _mongoDBService.GetUserByIdAsync(request.UserId);
                var accessTokenExpirationInSeconds = Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationInSeconds"]);

                // Generate a new access token for the user
                var newAccessToken = _jwtTokenService.GenerateToken(user, accessTokenExpirationInSeconds);

                // Return the new access token to the client
                return Ok(new { AccessToken = newAccessToken });
            }

            // If the refresh token is invalid, return an Unauthorized response
            return Unauthorized("Invalid or expired refresh token.");
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {

            //var users = await _mongoDBService.GetAllUsersAsync();
            //var userList = users.Select(u => new { u.Id, u.Username }).ToList();
            //return Ok(userList);

            try
            {
                // Fetch all users from the MongoDBService
                var users = await _mongoDBService.GetAllUsersAsync();

                // Check if users were found
                if (users == null || users.Count == 0)
                {
                    Console.WriteLine("No users found.");
                    return NotFound("No users found.");
                }

                // Create a user list with only Id and Username to return
                var userList = users.Select(u => new { u.Id, u.Username }).ToList();

                Console.WriteLine ($"{userList.Count} users successfully retrieved.");
                return Ok(userList);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 status code with error message
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching users.");
            }
        }
    }
}
