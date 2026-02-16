namespace AzureEntra.Functions
{
    using Core.Configuration;
    using Core.Constants;
    using Infrastructure.Graph;
    using Infrastructure.Persistence;
    using Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

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
    public class HttpGetAzureEntraUsers
    {
        private readonly AzureEntraUserService _userService;
        private readonly ILogger<HttpGetAzureEntraUsers> _logger;

        public HttpGetAzureEntraUsers(AzureEntraUserService userService, ILogger<HttpGetAzureEntraUsers> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("HttpGetAzureEntraUsers")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("HttpGetAzureEntraUsers triggered.");

            // Determine if EF Core should be used based on query parameter
            var useEfCore = !string.IsNullOrEmpty(req.Query["useEfCore"]) && 
                           req.Query["useEfCore"].ToString().ToLower() == "true";

            try
            {
                // Execute business logic
                // Use GraphFields.All to fetch all user properties
                var result = await _userService.SynchronizeUsersAsync(GraphFields.All, useEfCore)
                    .ConfigureAwait(false);

                // Return HTTP response
                if (result.Success)
                {
                    _logger.LogInformation("Synchronization successful.");
                    return new OkObjectResult(new
                    {
                        success = true,
                        message = result.Message,
                        data = new
                        {
                            userCount = result.UserCount,
                            rowsAffected = result.RowsAffected,
                            executionTimeMs = result.ExecutionTime.TotalMilliseconds,
                            persistenceMethod = useEfCore ? "EF Core" : "Direct Provider"
                        }
                    });
                }

                _logger.LogWarning("Synchronization completed with no data.");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Synchronization failed",
                    error = "No users processed"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration error during synchronization.");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    error = "Configuration error",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during synchronization.");
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