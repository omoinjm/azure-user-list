using GetAzureADUsers.Core.Models;
using System.Threading.Tasks;

namespace GetAzureADUsers.Infrastructure.Graph
{
    /// <summary>
    /// Contract for fetching Azure AD users from Microsoft Graph API.
    /// 
    /// WHY: Abstracts Graph API details, enables:
    /// - Easy testing (mock the interface)
    /// - Supporting multiple user sources
    /// - Encapsulating pagination logic
    /// - Isolating Graph SDK from business logic
    /// </summary>
    public interface IAzureADUserFetcher
    {
        /// <summary>
        /// Fetches all users from Azure AD.
        /// 
        /// Handles pagination automatically - returns all users regardless
        /// of how many pages exist in Graph API.
        /// </summary>
        /// <param name="selectFields">
        /// Optional Graph API fields to select.
        /// If null, fetches all fields defined in GraphFields.All
        /// If specified, only requested fields are returned (more efficient).
        /// Example: new[] { "id", "displayName", "mail" }
        /// </param>
        /// <returns>QueryResult containing users and pagination info.</returns>
        Task<QueryResult> FetchUsersAsync(string[] selectFields = null);
    }
}
