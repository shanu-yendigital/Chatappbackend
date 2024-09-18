using apiforchat.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace apiforchat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository;

        public ChatController(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        [HttpGet("getMessages")]
        public async Task<IActionResult> GetMessages([FromQuery] string senderId, [FromQuery] string receiverId)
        {
            var messages = await _messageRepository.GetMessagesAsync(senderId, receiverId);
            return Ok(messages);  // Return the list of ChatMessageModel
        }
    }


}
