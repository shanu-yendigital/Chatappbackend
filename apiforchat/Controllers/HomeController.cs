using apiforchat.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text; // directive to use Encoding
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserConnectionManager _userConnectionManager;

    public HomeController(ILogger<HomeController> logger, UserConnectionManager userConnectionManager)
    {
        _logger = logger;
        _userConnectionManager = userConnectionManager;
    }
    // Basic endpoint to check if the application is running
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Get endpoint was called.");
        return Ok("Chat application is running...");
    }
    // Endpoint to send a message from one user to another
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(string fromUserId, string toUserId, string message)
    {
        _logger.LogInformation($"SendMessage called by {fromUserId} to {toUserId}");

        // Retrieve the WebSocket connection for the target user
        var toUserSocket = _userConnectionManager.GetConnection(toUserId);
        if (toUserSocket == null || toUserSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning($"User {toUserId} is not open");
            return BadRequest("User is not connected");
        }

        // Encode the message as a byte array and send it to the target user
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var messageSegment = new ArraySegment<byte>(messageBytes);

        await toUserSocket.SendAsync(messageSegment, WebSocketMessageType.Text, true, CancellationToken.None);
        _logger.LogInformation($"Message from {fromUserId} to {toUserId} sent successfully");

        return Ok("Message sent");
    }

    

    }
