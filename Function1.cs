using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace get_azuread_users
{
    public class Item
    {
        public int AzureUserQueryId { get; set; }
        public DateTime? QueryDate { get; set; }
        public string QueryResult { get; set; }
    }

    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, [Sql("dbo.TR_AzureUserQuery", ConnectionStringSetting = "SqlConnectionString")] IAsyncCollector<Item> Items)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Machine));
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.User));
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Process));

            // Values from app registration
            // https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider
            var clientId = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Process);
            var tenantId = System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID", EnvironmentVariableTarget.Process);
            var clientSecret = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET", EnvironmentVariableTarget.Process);

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            GraphServiceClient graphServiceClient = new GraphServiceClient(clientSecretCredential);
            var graphResult = graphServiceClient.Users.Request().GetAsync().Result;

            var jsonResult = new JsonResult(graphResult);
            var result = JsonConvert.SerializeObject(jsonResult.Value);

            Item item = new() { QueryDate = DateTime.Now, QueryResult = result };

            await Items.AddAsync(item);
            await Items.FlushAsync();

            return new JsonResult(item);
        }
    }
}
