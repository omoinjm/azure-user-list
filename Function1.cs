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
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID", EnvironmentVariableTarget.Process));
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_TENANT_ID", EnvironmentVariableTarget.Process));
            log.LogInformation(System.Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET", EnvironmentVariableTarget.Process));
            log.LogInformation(System.Environment.GetEnvironmentVariable("SqlConnectionString", EnvironmentVariableTarget.Process));

            // Convert Graph result into Json
            var jsonResult = new JsonResult(AuthGraphClient.GraphClientResult());
            var result = JsonConvert.SerializeObject(jsonResult.Value);

            // Save to database
            var connectionString = System.Environment.GetEnvironmentVariable("SqlConnectionString", EnvironmentVariableTarget.Process);
            InsertToDB.SaveData(connectionString, result, log);

            return new JsonResult(AuthGraphClient.GraphClientResult());
        }
    }
}
