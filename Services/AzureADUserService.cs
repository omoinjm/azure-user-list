using GetAzureADUsers.Core.Configuration;
using GetAzureADUsers.Core.Models;
using GetAzureADUsers.Infrastructure.Graph;
using GetAzureADUsers.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GetAzureADUsers.Services
{
    /// <summary>
    /// Application service coordinating Azure AD user synchronization.
    /// 
    /// Responsibility: Orchestrate the flow
    /// - Fetch users from Azure AD
    /// - Transform/map if needed
    /// - Persist to database
    /// - Handle errors with context
    /// 
    /// NOT responsible for:
    /// - Graph SDK details (delegated to IAzureADUserFetcher)
    /// - SQL details (delegated to IAzureUserRepository)
    /// - Configuration retrieval (delegated to IAzureADConfiguration)
    /// 
    /// WHY: Clean separation allows independent testing of each concern.
    /// Mock the interfaces in unit tests, test service logic in isolation.
    /// </summary>
    public class AzureADUserService
    {
        private readonly IAzureADUserFetcher _userFetcher;
        private readonly IAzureUserRepository _repository;
        private readonly IAzureADConfiguration _config;
        private readonly ILogger _logger;

        public AzureADUserService(
            IAzureADUserFetcher userFetcher,
            IAzureUserRepository repository,
            IAzureADConfiguration config,
            ILogger logger = null)
        {
            _userFetcher = userFetcher ?? throw new ArgumentNullException(nameof(userFetcher));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        /// <summary>
        /// Synchronizes users from Azure AD to the database.
        /// 
        /// Flow:
        /// 1. Fetch users from Graph API
        /// 2. Validate results
        /// 3. Persist to database
        /// 4. Return result with statistics
        /// </summary>
        /// <param name="selectFields">Optional Graph fields to select. 
        /// If null, fetches all fields (from GraphFields.All)</param>
        /// <returns>SyncResult with success status and statistics.</returns>
        public async Task<SyncResult> SynchronizeUsersAsync(string[] selectFields = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger?.LogInformation("Starting user synchronization from Azure AD.");

                // Step 1: Fetch users from Graph API
                var queryResult = await _userFetcher.FetchUsersAsync(selectFields)
                    .ConfigureAwait(false);

                if (queryResult?.Users == null || queryResult.Users.Count == 0)
                {
                    _logger?.LogWarning("No users returned from Azure AD Graph API.");
                    return new SyncResult
                    {
                        Success = true,
                        UserCount = 0,
                        RowsAffected = 0,
                        Message = "No users found in Azure AD.",
                        ExecutionTime = DateTime.UtcNow - startTime
                    };
                }

                _logger?.LogInformation("Fetched {UserCount} users from Azure AD.", 
                    queryResult.Users.Count);

                // Step 2: Persist to database
                var rowsAffected = await _repository.SaveUsersAsync(queryResult.Users)
                    .ConfigureAwait(false);

                var executionTime = DateTime.UtcNow - startTime;

                _logger?.LogInformation(
                    "User synchronization completed successfully. " +
                    "Users: {UserCount}, Rows: {RowsAffected}, Duration: {Duration}ms",
                    queryResult.Users.Count, rowsAffected, executionTime.TotalMilliseconds);

                return new SyncResult
                {
                    Success = true,
                    UserCount = queryResult.Users.Count,
                    RowsAffected = rowsAffected,
                    Message = $"Successfully synchronized {queryResult.Users.Count} users.",
                    ExecutionTime = executionTime
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogError(ex, "Configuration or operational error during sync.");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during user synchronization.");
                throw;
            }
        }
    }

    /// <summary>
    /// Result of a user synchronization operation.
    /// 
    /// WHY: Provides structured result instead of returning raw data.
    /// Includes metadata (success, counts, execution time) for monitoring.
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// Whether the synchronization succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of users fetched from Azure AD.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Number of database rows affected.
        /// </summary>
        public int RowsAffected { get; set; }

        /// <summary>
        /// Human-readable status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Total execution time of the synchronization.
        /// Useful for monitoring performance.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }
}
