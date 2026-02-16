using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Contract for persisting Azure Entra users to storage using Entity Framework Core.
    /// </summary>
    public interface IEfAzureEntraUserRepository
    {
        /// <summary>
        /// Saves a collection of Azure Entra users to persistent storage.
        /// </summary>
        /// <param name="users">Collection of users to persist.</param>
        /// <returns>Number of records affected.</returns>
        Task<int> SaveUsersAsync(IEnumerable<AzureEntraUser> users);
        
        /// <summary>
        /// Applies any pending migrations to the database.
        /// </summary>
        Task EnsureDatabaseSchemaAsync();
    }
}