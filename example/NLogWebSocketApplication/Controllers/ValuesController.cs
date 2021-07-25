using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NLogWebSocketApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public ILogger Logger { get; set; }

        [HttpGet]
        public async Task<string> Get()
        {
            Logger.LogInformation($"Get 123");
            return "123";
        }
    }
}
