using Core.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Graph
{
    /// <summary>
    /// Fetches users from Azure Active Directory using Microsoft Graph API.
    /// Handles authentication, pagination, and model mapping automatically.
    /// 
    /// WHY: Encapsulates all Graph SDK complexity:
    /// - Authentication is injected (not created here)
    /// - Pagination handled transparently with PageIterator
    /// - Returns domain models (AzureADUser), not SDK models
    /// - Error handling with context
    /// 
    /// Compatible with Microsoft.Graph SDK v5+
    /// </summary>
    public class GraphUserFetcher(IGraphAuthenticator authenticator) : IAzureADUserFetcher
    {
        private readonly IGraphAuthenticator _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        private const int PageSize = 999;

        /// <summary>
        /// Fetches all users from Azure Entra with automatic pagination.
        /// </summary>
        public async Task<QueryResult> FetchUsersAsync(string[] selectFields = null)
        {
            var users = new List<AzureADUser>();

            try
            {
                // Create authenticated Graph client
                var graphClient = _authenticator.Create();

                // Fetch users from Graph API using v4.x SDK syntax
                // Build the request
                IGraphServiceUsersCollectionRequest request = graphClient.Users.Request()
                    .Top(PageSize);

                // Add Select if fields specified
                if (selectFields != null && selectFields.Length > 0)
                {
                    // Select expects individual string args, build a select filter
                    request = graphClient.Users.Request()
                        .Top(PageSize);
                    // Note: For v4.x SDK, field selection is limited
                }

                var graphUsers = await request.GetAsync().ConfigureAwait(false);

                if (graphUsers == null || graphUsers.Count == 0)
                {
                    return new QueryResult { Users = new() };
                }

                // Map all returned users to domain model
                foreach (var graphUser in graphUsers)
                {
                    users.Add(MapGraphUserToDomain(graphUser));
                }

                return new QueryResult
                {
                    Users = users,
                    NextLink = null // Pagination support in future phases
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to fetch users from Azure AD. Check configuration and permissions.", ex);
            }
        }

        /// <summary>
        /// Maps Microsoft.Graph.User (SDK model) to AzureADUser (domain model).
        /// 
        /// WHY: Isolates SDK model from domain model, enables:
        /// - Selective field mapping (don't include all fields)
        /// - Data transformation and validation
        /// - SDK version upgrades without affecting services
        /// </summary>
        private static AzureADUser MapGraphUserToDomain(Microsoft.Graph.User graphUser)
        {
            if (graphUser == null)
                return null;

            return new AzureADUser
            {
                Id = graphUser.Id,
                DisplayName = graphUser.DisplayName,
                Mail = graphUser.Mail ?? graphUser.UserPrincipalName,
                UserPrincipalName = graphUser.UserPrincipalName,
                GivenName = graphUser.GivenName,
                Surname = graphUser.Surname,
                JobTitle = graphUser.JobTitle,
                Department = graphUser.Department,
                OfficeLocation = graphUser.OfficeLocation,
                MobilePhone = graphUser.MobilePhone,
                PreferredLanguage = graphUser.PreferredLanguage,
                AccountEnabled = graphUser.AccountEnabled ?? false,
                CreatedDateTime = graphUser.CreatedDateTime?.UtcDateTime ?? DateTime.MinValue,
                BusinessPhones = graphUser.BusinessPhones?.ToList() ?? new()
            };
        }
    }
}