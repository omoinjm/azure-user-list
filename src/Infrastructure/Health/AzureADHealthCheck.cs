using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Health
{
    /// <summary>
    /// Health check implementation for Azure AD connectivity.
    /// </summary>
    public class AzureADHealthCheck : IHealthCheck
    {
        private readonly Func<Task<bool>> _healthCheckFunction;

        public AzureADHealthCheck(Func<Task<bool>> healthCheckFunction)
        {
            _healthCheckFunction = healthCheckFunction ?? throw new ArgumentNullException(nameof(healthCheckFunction));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _healthCheckFunction();
                
                if (isHealthy)
                {
                    return HealthCheckResult.Healthy("Azure AD connectivity is healthy.");
                }
                
                return HealthCheckResult.Unhealthy("Azure AD connectivity is unhealthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure AD connectivity check failed.", ex);
            }
        }
    }
}