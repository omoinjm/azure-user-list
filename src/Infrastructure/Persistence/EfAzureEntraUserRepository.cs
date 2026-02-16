using Core.Models;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Entity Framework Core implementation of Azure user repository.
    /// Works with both PostgreSQL and SQL Server contexts.
    /// </summary>
    public class EfAzureEntraUserRepository : IEfAzureEntraUserRepository
    {
        private readonly AzureEntraDbContext _context;
        private readonly ILogger<EfAzureEntraUserRepository> _logger;

        public EfAzureEntraUserRepository(AzureEntraDbContext context, ILogger<EfAzureEntraUserRepository> logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Persists users to the database using Entity Framework Core.
        /// </summary>
        /// <param name="users">Collection of Azure Entra users to persist.</param>
        /// <returns>Number of records affected in database.</returns>
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

                // Create the entity to store
                var userRecord = new AzureUserRecord
                {
                    QueryDate = DateTime.UtcNow,
                    QueryResult = json
                };

                // Add to context
                _context.AzureUserRecords.Add(userRecord);

                // Save changes asynchronously
                var rowsAffected = await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger?.LogInformation(
                    "Successfully saved {UserCount} users to database. " +
                    "Rows affected: {RowsAffected}",
                    userList.Count, rowsAffected);

                return rowsAffected;
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex,
                    "Entity Framework error while saving {UserCount} users.",
                    userList.Count);

                throw new InvalidOperationException(
                    "Failed to save users to database due to EF Core error.", ex);
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
        /// Ensures the database schema is up to date by applying any pending migrations.
        /// </summary>
        public async Task EnsureDatabaseSchemaAsync()
        {
            // Apply any pending migrations
            await _context.Database.MigrateAsync().ConfigureAwait(false);
        }
    }
}