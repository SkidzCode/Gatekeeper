using Microsoft.AspNetCore.Mvc;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Account;
using Microsoft.AspNetCore.Authorization;


namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly IEmailService _emailService;

        public EmailController(ILogger<EmailController> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
        {
            try
            {
                await _emailService.SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }


}