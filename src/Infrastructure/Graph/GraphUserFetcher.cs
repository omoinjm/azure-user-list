using Core.Models;
using Infrastructure.Utilities;
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
    /// - Returns domain models (AzureEntraUser), not SDK models
    /// - Error handling with context
    /// - Retry logic for transient failures
    ///
    /// Compatible with Microsoft.Graph SDK v5+
    /// </summary>
    public class GraphUserFetcher(IGraphAuthenticator authenticator) : IAzureEntraUserFetcher
    {
        private readonly IGraphAuthenticator _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        private const int PageSize = 999;
        private const int MaxRetries = 3;

        /// <summary>
        /// Fetches all users from Azure Entra with automatic pagination.
        /// </summary>
        public async Task<QueryResult> FetchUsersAsync(string[] selectFields = null)
        {
            try
            {
                return await RetryPolicy.ExecuteAsync(async () =>
                {
                    var users = new List<AzureEntraUser>();
                    string nextPageUrl = null;

                    // Create authenticated Graph client
                    var graphClient = _authenticator.Create();

                    // Build the initial request
                    var requestBuilder = graphClient.Users
                        .Request()
                        .Top(PageSize);

                    // Add Select if fields specified
                    if (selectFields != null && selectFields.Length > 0)
                    {
                        requestBuilder = requestBuilder.Select(selectFields);
                    }

                    // Get the first page
                    var currentPage = await requestBuilder.GetAsync().ConfigureAwait(false);

                    // Process the first page
                    if (currentPage != null)
                    {
                        foreach (var graphUser in currentPage)
                        {
                            users.Add(MapGraphUserToDomain(graphUser));
                        }

                        // Get the next page URL if it exists
                        nextPageUrl = currentPage.NextPageRequest?.RequestUrl;
                    }

                    // Process additional pages if they exist
                    while (currentPage?.NextPageRequest != null)
                    {
                        currentPage = await currentPage.NextPageRequest.GetAsync().ConfigureAwait(false);
                        
                        if (currentPage != null)
                        {
                            foreach (var graphUser in currentPage)
                            {
                                users.Add(MapGraphUserToDomain(graphUser));
                            }
                            
                            // Update the next page URL for the next iteration
                            nextPageUrl = currentPage.NextPageRequest?.RequestUrl;
                        }
                        else
                        {
                            break; // No more pages
                        }
                    }

                    return new QueryResult
                    {
                        Users = users,
                        NextLink = nextPageUrl
                    };
                }, MaxRetries).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to fetch users from Azure AD. Check configuration and permissions.", ex);
            }
        }

        /// <summary>
        /// Maps Microsoft.Graph.User (SDK model) to AzureEntraUser (domain model).
        /// 
        /// WHY: Isolates SDK model from domain model, enables:
        /// - Selective field mapping (don't include all fields)
        /// - Data transformation and validation
        /// - SDK version upgrades without affecting services
        /// </summary>
        private static AzureEntraUser MapGraphUserToDomain(Microsoft.Graph.User graphUser)
        {
            if (graphUser == null)
                return null;

            return new AzureEntraUser
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