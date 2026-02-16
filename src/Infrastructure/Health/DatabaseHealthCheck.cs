using Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Health
{
    /// <summary>
    /// Health check implementation for database connectivity using service provider.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AzureEntraDbContext>();
                
                // Attempt to connect to the database
                var isConnected = await dbContext.Database.CanConnectAsync(cancellationToken);
                
                if (isConnected)
                {
                    return HealthCheckResult.Healthy("Database connectivity is healthy.");
                }
                
                return HealthCheckResult.Unhealthy("Database connectivity is unhealthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connectivity check failed.", ex);
            }
        }
    }
}