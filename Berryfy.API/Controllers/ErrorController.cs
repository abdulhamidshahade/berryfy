using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Berryfy.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [AllowAnonymous]
    [DisableRateLimiting]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("/error")]
        public IActionResult HandleError([FromServices] IHostEnvironment environment)
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (feature?.Error != null)
            {
                _logger.LogError(feature.Error, "Unhandled exception");
            }

            if (environment.IsDevelopment())
            {
                return Problem(
                    title: "An error occurred",
                    detail: feature?.Error?.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Problem(
                title: "An error occurred",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
