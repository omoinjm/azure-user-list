using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AzureEntra.Functions
{
    public class HealthCheckFunction
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthCheckFunction(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [Function("HealthCheck")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            var report = await _healthCheckService.CheckHealthAsync();

            // Build dynamic response object for all health check entries
            var entriesDict = new Dictionary<string, object>();
            foreach (var entry in report.Entries)
            {
                entriesDict[entry.Key.ToLower()] = new
                {
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration,
                    Tags = entry.Value.Tags,
                    Data = entry.Value.Data
                };
            }

            var response = new
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration,
                Entries = entriesDict
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return report.Status == HealthStatus.Healthy
                ? new OkObjectResult(json)
                : new ObjectResult(json) { StatusCode = 503 };
        }
    }
}