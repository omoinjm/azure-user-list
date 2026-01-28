using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;

namespace Graph.Helper
{
    internal class AuthGraphClient
    {
        // Creates an authorized connection to Azure Active Directory using a client secret that was generated for an App Registration.
        public static async System.Threading.Tasks.Task<IGraphServiceUsersCollectionPage> GraphClientResult(ILogger log)
        {
            try
            {
                // Values from app registration using environment variables.
                // https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider
                var clientId = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Process);
                var tenantId = System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID", EnvironmentVariableTarget.Process);
                var clientSecret = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET", EnvironmentVariableTarget.Process);

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Azure credentials are not configured. Please set AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_CLIENT_SECRET.");
                }

                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                // The client credential flow enables service applications to run without user interaction.
                // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
                GraphServiceClient graphServiceClient = new GraphServiceClient(clientSecretCredential);
                return await graphServiceClient.Users.Request().GetAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error retrieving Azure AD users from Microsoft Graph API");
                throw;
            }
        }
    }
}