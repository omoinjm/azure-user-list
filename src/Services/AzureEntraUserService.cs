using Core.Configuration;
using Core.Models;
using Infrastructure.Graph;
using Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    /// <summary>
    /// Application service coordinating Azure Entra user synchronization.
    ///
    /// Responsibility: Orchestrate the flow
    /// - Fetch users from Azure Entra
    /// - Transform/map if needed
    /// - Persist to database
    /// - Handle errors with context
    ///
    /// NOT responsible for:
    /// - Graph SDK details (delegated to IAzureEntraUserFetcher)
    /// - SQL details (delegated to IAzureEntraUserRepository)
    /// - Configuration retrieval (delegated to IAzureEntraConfiguration)
    ///
    /// WHY: Clean separation allows independent testing of each concern.
    /// Mock the interfaces in unit tests, test service logic in isolation.
    /// </summary>
    public class AzureEntraUserService(
        IAzureEntraUserFetcher userFetcher,
        IAzureEntraUserRepository repository,
        IEfAzureEntraUserRepository efRepository,
        IAzureEntraConfiguration config,
        ILogger logger = null)
    {
        private readonly IAzureEntraUserFetcher _userFetcher = userFetcher ?? throw new ArgumentNullException(nameof(userFetcher));
        private readonly IAzureEntraUserRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        private readonly IEfAzureEntraUserRepository _efRepository = efRepository ?? throw new ArgumentNullException(nameof(efRepository));
        private readonly IAzureEntraConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly ILogger _logger = logger;

        /// <summary>
        /// Synchronizes users from Azure Entra to the database.
        ///
        /// Flow:
        /// 1. Fetch users from Graph API
        /// 2. Validate results
        /// 3. Persist to database
        /// 4. Return result with statistics
        /// </summary>
        /// <param name="selectFields">Optional Graph fields to select.
        /// If null, fetches all fields (from GraphFields.All)</param>
        /// <param name="useEfCore">Whether to use EF Core for persistence (defaults to false for legacy providers)</param>
        /// <returns>SyncResult with success status and statistics.</returns>
        public async Task<SyncResult> SynchronizeUsersAsync(string[] selectFields = null, bool useEfCore = false)
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
                int rowsAffected;
                if (useEfCore)
                {
                    // Use EF Core repository
                    await _efRepository.EnsureDatabaseSchemaAsync().ConfigureAwait(false);
                    rowsAffected = await _efRepository.SaveUsersAsync(queryResult.Users)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Use legacy repository
                    rowsAffected = await _repository.SaveUsersAsync(queryResult.Users)
                        .ConfigureAwait(false);
                }

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
}