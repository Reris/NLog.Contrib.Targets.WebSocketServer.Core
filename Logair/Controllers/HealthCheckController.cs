using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Logair.Controllers;

[Route("healthz")]
public class HealthCheckController : Controller
{
    // GET healthcheck
    [HttpGet]
    public string Info()
    {
        var p = Process.GetCurrentProcess();
        return $"Running since: {p.StartTime}, Used: {p.TotalProcessorTime}";
    }
}
