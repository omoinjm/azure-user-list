using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Health
{
    /// <summary>
    /// General application health check that verifies basic application functionality.
    /// </summary>
    public class ApplicationHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Perform basic application health checks
                // For example, check if critical services are running
                var startTime = DateTime.UtcNow;
                
                // Simulate a quick check (in a real app, this might check cache, etc.)
                await Task.Delay(1, cancellationToken);
                
                var duration = DateTime.UtcNow - startTime;
                
                if (duration.TotalMilliseconds < 100) // Simple threshold check
                {
                    return HealthCheckResult.Healthy("Application is healthy.", 
                        data: new Dictionary<string, object> { { "response_time_ms", duration.TotalMilliseconds } });
                }
                else
                {
                    return HealthCheckResult.Degraded("Application response time is elevated.", 
                        data: new Dictionary<string, object> { { "response_time_ms", duration.TotalMilliseconds } });
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Application health check failed.", ex);
            }
        }
    }
}