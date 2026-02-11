using Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// SQL Server implementation of Azure user repository.
    /// 
    /// WHY: Encapsulates all SQL-specific logic:
    /// - Connection management
    /// - Command execution
    /// - Parameterized queries (prevents SQL injection)
    /// - Serialization/deserialization
    /// - Async operations
    /// 
    /// This enables:
    /// - Services don't know about SQL
    /// - Easy testing (mock IAzureUserRepository)
    /// - Easy migration to different database
    /// </summary>
    public class SqlAzureUserRepository : IAzureUserRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private const string TableName = "[dbo].[TR_AzureUserQuery]";

        public SqlAzureUserRepository(string connectionString, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), 
                    "SQL connection string cannot be null or empty.");

            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Persists users to SQL Server database.
        /// 
        /// Uses parameterized queries to prevent SQL injection attacks.
        /// Serializes user list to JSON for storage.
        /// </summary>
        /// <param name="users">Collection of Azure AD users to persist.</param>
        /// <returns>Number of rows affected in database.</returns>
        public async Task<int> SaveUsersAsync(IEnumerable<AzureADUser> users)
        {
            if (users == null)
                throw new ArgumentNullException(nameof(users));

            var userList = users.ToList();
            if (!userList.Any())
            {
                _logger?.LogWarning("SaveUsersAsync called with empty user collection.");
                return 0;
            }

            try
            {
                // Serialize users to JSON
                var json = JsonConvert.SerializeObject(userList);

                // Use async SQL connection
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    // Use parameterized query to prevent SQL injection
                    using (var command = new SqlCommand(
                        $"INSERT INTO {TableName} (QueryDate, QueryResult) " +
                        "VALUES (@queryDate, @queryResult)",
                        connection))
                    {
                        // Add parameters (never use string interpolation for SQL values!)
                        command.Parameters.AddWithValue("@queryDate", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@queryResult", json);

                        int rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                        _logger?.LogInformation(
                            "Successfully saved {UserCount} users to database. " +
                            "Table: {TableName}, Rows affected: {RowsAffected}",
                            userList.Count, TableName, rowsAffected);

                        return rowsAffected;
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex,
                    "SQL error while saving {UserCount} users. " +
                    "Connection: {ConnectionString}, Error: {SqlError}",
                    userList.Count, _connectionString.Substring(0, 30) + "***", ex.Message);

                throw new InvalidOperationException(
                    "Failed to save users to database due to SQL error.", ex);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "JSON serialization error while saving {UserCount} users.",
                    userList.Count);

                throw new InvalidOperationException(
                    "Failed to serialize users for database storage.", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Unexpected error while saving {UserCount} users to database.",
                    userList.Count);

                throw new InvalidOperationException(
                    "Unexpected error while saving users to database.", ex);
            }
        }
    }
}