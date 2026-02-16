using Core.Models;
using Npgsql;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// PostgreSQL implementation of Azure user repository.
    ///
    /// WHY: Encapsulates all PostgreSQL-specific logic:
    /// - Connection management
    /// - Command execution
    /// - Parameterized queries (prevents SQL injection)
    /// - Serialization/deserialization
    /// - Async operations
    ///
    /// This enables:
    /// - Services don't know about PostgreSQL
    /// - Easy testing (mock IAzureEntraUserRepository)
    /// - Easy migration to different database
    /// </summary>
    public class PostgreSqlAzureUserRepository : IAzureEntraUserRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private const string TableName = "tr_azure_user_query"; // Using snake_case for PostgreSQL convention

        public PostgreSqlAzureUserRepository(string connectionString, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString),
                    "PostgreSQL connection string cannot be null or empty.");

            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Persists users to PostgreSQL database.
        ///
        /// Uses parameterized queries to prevent SQL injection attacks.
        /// Serializes user list to JSON for storage using PostgreSQL's JSONB type.
        /// </summary>
        /// <param name="users">Collection of Azure AD users to persist.</param>
        /// <returns>Number of rows affected in database.</returns>
        public async Task<int> SaveUsersAsync(IEnumerable<AzureEntraUser> users)
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

                // Use async PostgreSQL connection
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    // Use parameterized query to prevent SQL injection
                    using (var command = new NpgsqlCommand(
                        $"INSERT INTO {TableName} (query_date, query_result) " +
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
            catch (NpgsqlException ex)
            {
                _logger?.LogError(ex,
                    "PostgreSQL error while saving {UserCount} users. " +
                    "Connection: {ConnectionString}, Error: {PgError}",
                    userList.Count, _connectionString.Substring(0, 30) + "***", ex.Message);

                throw new InvalidOperationException(
                    "Failed to save users to database due to PostgreSQL error.", ex);
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
        
        /// <summary>
        /// Creates the required table if it doesn't exist.
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (var command = new NpgsqlCommand($@"
                    CREATE TABLE IF NOT EXISTS {TableName} (
                        id SERIAL PRIMARY KEY,
                        query_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        query_result JSONB NOT NULL
                    )", connection))
                {
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}