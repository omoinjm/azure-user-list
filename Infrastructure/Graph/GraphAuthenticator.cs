using Azure.Identity;
using GetAzureADUsers.Core.Configuration;
using Microsoft.Graph;

namespace GetAzureADUsers.Infrastructure.Graph
{
    /// <summary>
    /// Creates authenticated Graph Service Clients using Client Credentials flow.
    /// 
    /// WHY: Encapsulates all Graph SDK authentication details:
    /// - Token credential creation
    /// - Authority host configuration
    /// - GraphServiceClient initialization
    /// 
    /// This enables:
    /// - Easy testing (mock IGraphAuthenticator)
    /// - Support for multiple auth strategies
    /// - Centralized authentication logic
    /// </summary>
    public class GraphAuthenticator : IGraphAuthenticator
    {
        private readonly IAzureADConfiguration _config;

        public GraphAuthenticator(IAzureADConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Creates and returns an authenticated GraphServiceClient using client credentials.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers
        /// 
        /// The client credential flow (OAuth 2.0) enables service applications to run
        /// without user interaction. Credentials are stored securely in Azure Key Vault
        /// or environment variables.
        /// </summary>
        /// <returns>Authenticated GraphServiceClient ready for API calls.</returns>
        public GraphServiceClient Create()
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // Create the credentials using client ID, tenant ID, and client secret
            // This is the service-to-service (S2S) authentication pattern
            var credential = new ClientSecretCredential(
                _config.TenantId,
                _config.ClientId,
                _config.ClientSecret,
                options);

            // Create and return the GraphServiceClient
            return new GraphServiceClient(credential);
        }
    }
}
