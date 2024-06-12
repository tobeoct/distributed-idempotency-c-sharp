using DistributedIdempotency.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Test.Api.Controllers
{
    public class Test
    {
        [IdempotencyKey]
        public string Reference { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class TestOrdering:Test
    {
        [IdempotencyKey(1)]
        public decimal Amount { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    [Idempotent]
    public class IdempotencyKeyController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<IdempotencyKeyController> _logger;

        public IdempotencyKeyController(ILogger<IdempotencyKeyController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "Get")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        
        [HttpGet]
        [Route("TestParameter")]
        public string TestParameter([IdempotencyKey] string reference, string timestamp)
        {
            return HttpContext.Items["IdempotencyKey"]?.ToString();
        }

        [HttpPost]
        [Route("TestParameter2")]
        public IActionResult TestParameter2([IdempotencyKey] string reference)
        {
            return Ok(HttpContext.Items["IdempotencyKey"]?.ToString());
        }
        [HttpPost]
        [Route("TestProperty")]
        public string TestProperty(Test request)
        {

            return HttpContext.Items["IdempotencyKey"]?.ToString();
        }


        [HttpPost]
        [Route("TestPropertyOrdering")]
        public string TestPropertyOrdering(TestOrdering request)
        {
            return HttpContext.Items["IdempotencyKey"]?.ToString();
        }

    }
}
