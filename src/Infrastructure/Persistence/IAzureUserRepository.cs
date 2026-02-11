using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Contract for persisting Azure AD users to storage.
    /// 
    /// WHY: Abstracts persistence layer, enables:
    /// - Easy swapping of storage (SQL → CosmosDB, etc.)
    /// - Testing without real database
    /// - Multiple persistence strategies
    /// - Centralized database logic
    /// </summary>
    public interface IAzureUserRepository
    {
        /// <summary>
        /// Saves a collection of Azure AD users to persistent storage.
        /// </summary>
        /// <param name="users">Collection of users to persist.</param>
        /// <returns>Number of rows/records affected.</returns>
        Task<int> SaveUsersAsync(IEnumerable<AzureADUser> users);
    }
}