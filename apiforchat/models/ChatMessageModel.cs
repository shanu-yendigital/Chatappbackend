using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace apiforchat.models
{
    public class ChatMessageModel
    {
        [BsonId] 
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
   
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
