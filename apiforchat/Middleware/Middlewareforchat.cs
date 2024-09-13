using apiforchat.Models;
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
    private readonly Dictionary<string, WebSocket> _userSocketMap = new Dictionary<string, WebSocket>();
    private readonly List<WebSocket> _sockets = new List<WebSocket>(); // List of active WebSockets conn
    private readonly Dictionary<WebSocket, string> _socketUserMap = new Dictionary<WebSocket, string>();
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(ILogger<WebSocketManager> logger)
    {
        _logger = logger;
    }
    // Method to add a new WebSocket connection and start receiving messages
    public async Task AddSocket(WebSocket socket, string userId)
    {
        _logger.LogInformation("Adding WebSocket for user: {UserId}", userId);
        _sockets.Add(socket); // Add socket to the list of active connectins
        _socketUserMap[socket] = userId;  // Map socket to the userID

        _logger.LogInformation("Total active WebSocket connections: {Count}", _sockets.Count);
        await Receive(socket);
    }

    public async Task RemoveSocket(WebSocket socket)
    {

        _sockets.Remove(socket);
        _socketUserMap.Remove(socket);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketManager", CancellationToken.None);
    }

    private async Task Receive(WebSocket socket)
    {
        var buffer = new byte[1024 * 4]; //buffer to hold data
        _logger.LogInformation("Started receiving messages from WebSocket.");
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            //received message
            _logger.LogInformation($"Message received: {message}");

            //  await BroadcastMessage(socket, message);

            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        _logger.LogInformation($"WebSocket connection closed for user: {_socketUserMap[socket]} with status: {result.CloseStatus}");
        await RemoveSocket(socket);
    }
    //private async Task BroadcastMessage(WebSocket senderSocket, string message)
    //{
    //    var senderId = _socketUserMap[senderSocket];
    //    var formattedMessage = $"{senderId}: {message}";

    //    _logger.LogInformation("Broadcasting message from user: {UserId} to all other connected users.", senderId);

    //    var buffer = Encoding.UTF8.GetBytes(formattedMessage);

    //    foreach (var socket in _sockets)
    //    {
    //        if (socket.State == WebSocketState.Open && socket != senderSocket)
    //        {
    //            _logger.LogInformation("Sending message to user: {UserId}", _socketUserMap[socket]);
    //            await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);

    //        }
    //    }
    //}

    private async Task SendMessageToUser(WebSocket senderSocket, string targetUserId, string message)
    {
        var senderId = _socketUserMap[senderSocket];
        var formattedMessage = $"{senderId}:{message}";
        _logger.LogInformation("Sending message from user: {SenderId} to user: {TargetUserId}", senderId, targetUserId);

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
