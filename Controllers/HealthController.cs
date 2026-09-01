using Microsoft.AspNetCore.Mvc;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "VerifyDriverAPI",
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}