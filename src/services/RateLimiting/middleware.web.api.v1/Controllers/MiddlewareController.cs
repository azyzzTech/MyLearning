using Microsoft.AspNetCore.Mvc;

namespace middleware.web.api.v1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MiddlewareController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Processed.");
        }
    }
}
