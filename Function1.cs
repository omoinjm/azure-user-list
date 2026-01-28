using Graph.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sql.Helper;
using System;

namespace GetAzureADUsers
{
    public static class Function1
    {
        [FunctionName("GetAzureADUsers")]
        public static async System.Threading.Tasks.Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("GetAzureADUsers function triggered.");

                // Retrieve Azure AD users from Microsoft Graph API
                var users = await AuthGraphClient.GraphClientResult(log);

                if (users == null)
                {
                    log.LogWarning("No users returned from Microsoft Graph API.");
                    return new OkObjectResult(new { message = "No users found" });
                }

                // Convert Graph result into Json
                var result = JsonConvert.SerializeObject(users);

                // Save to database
                var connectionString = System.Environment.GetEnvironmentVariable("SqlConnectionString", EnvironmentVariableTarget.Process);
                if (string.IsNullOrEmpty(connectionString))
                {
                    log.LogError("SqlConnectionString is not configured.");
                    return new BadRequestObjectResult("Database connection is not configured.");
                }

                InsertToDB.SaveData(connectionString, result, log);

                return new OkObjectResult(users);
            }
            catch (InvalidOperationException ex)
            {
                log.LogError(ex, "Configuration error: {Message}", ex.Message);
                return new BadRequestObjectResult("Azure credentials are not configured properly.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the request.");
                return new ObjectResult(new { error = "An error occurred while retrieving users." })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
