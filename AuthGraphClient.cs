using Azure.Identity;
using Microsoft.Graph;
using System;

namespace Graph.Helper
{
    internal class AuthGraphClient
    {
        // Creates a autherized connection to Azure Active Directory using a client secret that was generated for an App Registration.
        public static IGraphServiceUsersCollectionPage GraphClientResult()
        {
            // Values from app registration using environment variables.
            // https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider
            var clientId = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Process);
            var tenantId = System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID", EnvironmentVariableTarget.Process);
            var clientSecret = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET", EnvironmentVariableTarget.Process);

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // The client credential flow enables service applications to run without user interaction.
            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            GraphServiceClient graphServiceClient = new GraphServiceClient(clientSecretCredential);
            return graphServiceClient.Users.Request().GetAsync().Result;
        }
    }
}