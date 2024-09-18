using apiforchat.models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace apiforchat.Repositories
{
    public interface IMessageRepository
    {
        Task SaveMessageAsync(ChatMessageModel message);
        Task<List<ChatMessageModel>> GetMessagesAsync(string senderId, string receiverId);
    }
    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<ChatMessageModel> _messageCollection;

        public MessageRepository(IMongoDatabase database)
        {
            _messageCollection = database.GetCollection<ChatMessageModel>("Messages");
        }

        public async Task SaveMessageAsync(ChatMessageModel message)
        {
            if (string.IsNullOrEmpty(message.Id))
            {
                message.Id = ObjectId.GenerateNewId().ToString(); // Generate a unique ObjectId
            }

            // Check if a message with the same Id already exists to avoid duplicate insertion
            var existingMessage = await _messageCollection.Find(m => m.Id == message.Id).FirstOrDefaultAsync();

            if (existingMessage == null)
            {
                // Insert the message if no duplicate is found
                await _messageCollection.InsertOneAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Message with the same ID already exists.");
            }
        }

        public async Task<List<ChatMessageModel>> GetMessagesAsync(string senderId, string receiverId)
        {
            return await _messageCollection.Find(msg =>
                        (msg.SenderId == senderId && msg.ReceiverId == receiverId) ||
                        (msg.SenderId == receiverId && msg.ReceiverId == senderId))
                        .SortBy(msg => msg.Timestamp)
                        .ToListAsync();
        }
    }
}
