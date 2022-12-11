using Microsoft.AspNetCore.Mvc;

namespace MyDrive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DriveController : ControllerBase
    {

        private readonly ILogger<DriveController> _logger;

        public DriveController(ILogger<DriveController> logger)
        {
            _logger = logger;
        }

    }
}