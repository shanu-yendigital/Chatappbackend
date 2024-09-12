using MongoDB.Bson;

namespace apiforchat.Models
{
    public class User
    {
        public ObjectId Id { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }
        public string Email { get; set; }

        

    }
}
