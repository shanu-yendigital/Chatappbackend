using apiforchat.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
namespace apiforchat.services

{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _users;
        private readonly ILogger<MongoDBService> _logger;

        public MongoDBService(IOptions<MongoDBSettings> settings, ILogger<MongoDBService> logger)
        {

            _logger = logger;
            _logger.LogInformation("Initializing MongoDBService");



            var mongoDBSettings = settings.Value;

            try
            {
                var client = new MongoClient(mongoDBSettings.ConnectionString);
                var database = client.GetDatabase(mongoDBSettings.DatabaseName);
                _users = database.GetCollection<User>(mongoDBSettings.UsersCollectionName);

                _logger.LogInformation("MongoDB Settings: {@MongoDBSettings}", mongoDBSettings);

                if (string.IsNullOrEmpty(mongoDBSettings.ConnectionString))
                {
                    throw new ArgumentNullException(nameof(mongoDBSettings.ConnectionString), "MongoDB connection string is null or empty.");
                }

                _logger.LogInformation("MongoDBService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing MongoDBService");
                throw;
            }
        }

        public async Task CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        // Method to authenticate a user
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);

            // Check if the user exists and the password is correct
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            // Convert the string userId to ObjectId
            if (!ObjectId.TryParse(userId, out var objectId))
            {
                return null; 
            }

            return await _users.Find(u => u.Id == objectId).FirstOrDefaultAsync();
        }
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users.Find(_ => true).ToListAsync();  // Fetch all users from MongoDB
        }
    }
}