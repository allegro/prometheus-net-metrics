using Microsoft.AspNetCore.Mvc;

namespace Allegro.Prometheus.TrueRpsMetric.Demo.Controllers;

[Route("demo")]
public class DemoController : ControllerBase
{
    [HttpGet]
    public ActionResult Demo()
    {
        return Ok();
    }
}