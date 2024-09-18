using apiforchat.models;
using apiforchat.Models;
using apiforchat.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;

public class MiddlewareforChat
{   
    private readonly RequestDelegate _next;
    private readonly WebSocketManager _webSocketManager;
    private readonly ILogger<MiddlewareforChat> _logger;
  

    public MiddlewareforChat(RequestDelegate next, WebSocketManager webSocketManager, ILogger<MiddlewareforChat> logger)
    {
        _next = next;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
   {
        _logger.LogInformation("Processing HTTP request: {Path}", context.Request.Path);

        // Check if req is for WebSocket connection
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogInformation("WebSocket request received");
                
                // Accepting the WebSocket connection
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var userId = context.Request.Query["userId"].ToString();
                await _webSocketManager.AddSocket(webSocket, userId);
            }
            else
            {
                context.Response.StatusCode = 400;
                _logger.LogWarning("Invalid WebSocket request.");
            }
        }
        else
        {
            await _next(context);
        }
    }
}
public class WebSocketManager
{
    private readonly IMessageRepository _messageRepository;
    private readonly Dictionary<string, WebSocket> _userSocketMap = new Dictionary<string, WebSocket>();
    private readonly List<WebSocket> _sockets = new List<WebSocket>();           // List of active WebSockets conn
    private readonly Dictionary<WebSocket, string> _socketUserMap = new Dictionary<WebSocket, string>();
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(ILogger<WebSocketManager> logger, IMessageRepository messageRepository)
    {
        _logger = logger;
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
    }
    // Method to add a new WebSocket connection and start receiving messages
    public async Task AddSocket(WebSocket socket, string userId)
    {
        _logger.LogInformation("Adding WebSocket for user: {UserId}", userId);
        foreach (var user in _userSocketMap.Keys)
        {
            _logger.LogInformation("User: {User}", user);
        }
        _sockets.Add(socket); // Add socket to the list of active connectins
        _userSocketMap[userId] = socket;
        _socketUserMap[socket] = userId;  // Map socket to the userID

        _logger.LogInformation("Total active WebSocket connections: {Count}", _sockets.Count);
        await Receive(socket);
    }

    public async Task RemoveSocket(WebSocket socket)
    {
        var userId = _socketUserMap[socket];
        _sockets.Remove(socket);
        _userSocketMap.Remove(userId);
        _socketUserMap.Remove(socket);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketManager", CancellationToken.None);
    }

   private async Task Receive(WebSocket socket)
    {
        var buffer = new byte[1024 * 4]; 
        _logger.LogInformation("Started receiving messages from WebSocket.");
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            
            _logger.LogInformation($"Message received: {message}");

           
            var parts = message.Split(':', 2);
            if (parts.Length == 2)
            {
                var targetUserId = parts[0]; // This is the receiver
                var textMessage = parts[1]; // This is the actual message

                // Get sender ID
                if (_socketUserMap.TryGetValue(socket, out var senderUserId))
                {
                    _logger.LogInformation("Sending message from user: {SenderId} to user: {TargetUserId}", senderUserId, targetUserId);
                    await SendMessageToUser(socket, targetUserId, textMessage);
                }
                else
                {
                    _logger.LogWarning("Sender user ID not found for the socket.");
                }
            }
            else
            {
                _logger.LogWarning("Received malformed message: {Message}", message);
            }

            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        _logger.LogInformation($"WebSocket connection closed for user: {_socketUserMap[socket]} with status: {result.CloseStatus}");
        await RemoveSocket(socket);
    }


    private async Task SendMessageToUser(WebSocket senderSocket, string targetUserId, string message)
    {
        if (senderSocket == null || string.IsNullOrEmpty(targetUserId) || string.IsNullOrEmpty(message))
        {
            _logger.LogError("Invalid input: senderSocket, targetUserId, or message is null.");
            return;
        }
        var senderId = _socketUserMap[senderSocket];

        if (string.IsNullOrEmpty(senderId))
        {
            _logger.LogError("Sender ID could not be found.");
            return;
        }
        var formattedMessage = $"{senderId}:{message}";
        _logger.LogInformation("Sending message from user: {SenderId} to user: {TargetUserId}", senderId, targetUserId);

      
        var chatMessage = new ChatMessageModel
        {
            SenderId = senderId,
            ReceiverId = targetUserId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
        await _messageRepository.SaveMessageAsync(chatMessage);

        if (_userSocketMap.ContainsKey(targetUserId))
        {
            var targetSocket = _userSocketMap[targetUserId];
            if (targetSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(formattedMessage);
                await targetSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), 
                                              WebSocketMessageType.Text, 
                                              true, 
                                              CancellationToken.None);
            }
            else
            {
                _logger.LogWarning("Target WebSocket for user: {TargetUserId} is not open.", targetUserId);
            }
        }
        else
        {
            _logger.LogWarning("User: {TargetUserId} is not connected.", targetUserId);
        }
    }

    
}
