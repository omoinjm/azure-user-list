using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AzureEntra.Functions
{
    public class AdvancedHealthCheckFunction
    {
        private readonly HealthCheckService _healthCheckService;

        public AdvancedHealthCheckFunction(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [Function("DetailedHealthCheck")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/detail")] HttpRequest req)
        {
            var report = await _healthCheckService.CheckHealthAsync();

            // Build detailed response object for all health check entries
            var entriesDict = new Dictionary<string, object>();
            foreach (var entry in report.Entries)
            {
                entriesDict[entry.Key.ToLower()] = new
                {
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration,
                    Tags = entry.Value.Tags,
                    Data = entry.Value.Data,
                    Exception = entry.Value.Exception?.Message // Only include message for security
                };
            }

            var response = new
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration,
                Timestamp = DateTime.UtcNow,
                Entries = entriesDict,
                TotalChecks = report.Entries.Count,
                FailedChecks = report.Entries.Count(e => e.Value.Status != HealthStatus.Healthy)
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return report.Status == HealthStatus.Healthy
                ? new OkObjectResult(json)
                : new ObjectResult(json) { StatusCode = 503 };
        }
        
        [Function("SimpleHealthCheck")]
        public async Task<IActionResult> SimpleHealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/simple")] HttpRequest req)
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                Status = report.Status.ToString(),
                Healthy = report.Status == HealthStatus.Healthy,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return report.Status == HealthStatus.Healthy
                ? new OkObjectResult(json)
                : new ObjectResult(json) { StatusCode = 503 };
        }
    }
}