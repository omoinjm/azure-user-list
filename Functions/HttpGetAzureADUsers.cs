using GetAzureADUsers.Core.Configuration;
using GetAzureADUsers.Core.Constants;
using GetAzureADUsers.Infrastructure.Graph;
using GetAzureADUsers.Infrastructure.Persistence;
using GetAzureADUsers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GetAzureADUsers.Functions
{
    /// <summary>
    /// Azure Function to retrieve and synchronize Azure AD users.
    /// 
    /// Entry point for HTTP requests to fetch Azure AD users and persist them to database.
    /// 
    /// Responsibility: ORCHESTRATION ONLY
    /// - Parse HTTP request
    /// - Build dependencies
    /// - Call service
    /// - Format HTTP response
    /// - Map HTTP errors
    /// 
    /// Does NOT contain:
    /// - Business logic
    /// - Graph SDK calls
    /// - Database operations
    /// 
    /// WHY: Functions are thin orchestrators. Real work happens in services.
    /// This makes the function easy to understand and test.
    /// 
    /// Authorization: Function-level (requires API key in x-functions-key header)
    /// </summary>
    public static class HttpGetAzureADUsers
    {
        [FunctionName("HttpGetAzureADUsers")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HttpGetAzureADUsers triggered.");

            try
            {
                // Step 1: Load configuration from environment
                var config = new AzureADConfiguration();

                // Step 2: Build infrastructure dependencies
                var authenticator = new GraphAuthenticator(config);
                var userFetcher = new GraphUserFetcher(authenticator);
                var repository = new SqlAzureUserRepository(config.SqlConnectionString, log);

                // Step 3: Create service
                var service = new AzureADUserService(userFetcher, repository, config, log);

                // Step 4: Execute business logic
                // Use GraphFields.All to fetch all user properties
                var result = await service.SynchronizeUsersAsync(GraphFields.All)
                    .ConfigureAwait(false);

                // Step 5: Return HTTP response
                if (result.Success)
                {
                    log.LogInformation("Synchronization successful.");
                    return new OkObjectResult(new
                    {
                        success = true,
                        message = result.Message,
                        data = new
                        {
                            userCount = result.UserCount,
                            rowsAffected = result.RowsAffected,
                            executionTimeMs = result.ExecutionTime.TotalMilliseconds
                        }
                    });
                }

                log.LogWarning("Synchronization completed with no data.");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Synchronization failed",
                    error = "No users processed"
                });
            }
            catch (InvalidOperationException ex)
            {
                log.LogError(ex, "Configuration error during synchronization.");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    error = "Configuration error",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected error during synchronization.");
                return new ObjectResult(new
                {
                    success = false,
                    error = "Internal server error",
                    message = "An unexpected error occurred during user synchronization."
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
