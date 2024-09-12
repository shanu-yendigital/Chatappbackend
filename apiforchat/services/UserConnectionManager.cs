using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace apiforchat.services
{
    // UserConnectionManager class responsible for managing WS connections
    // associated with different users,keeps track of which WS connection
    // belongs to which user by storing them in a concurrent dictionary
    public class UserConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();

        public void AddConnection(string userId, WebSocket socket)
        {
            _connections[userId] = socket;
        }

        public void RemoveConnection(string userId)
        {
            _connections.TryRemove(userId, out _);
        }

        public WebSocket GetConnection(string userId)
        {
            _connections.TryGetValue(userId, out var socket);
            return socket;
        }
    }
}
