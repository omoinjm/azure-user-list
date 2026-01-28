```
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Optix.Services.Zoota.Core;
using Optix.Services.Shared.Services;

namespace Optix.Services.Zoota.Functions
{
    public static class HttpGetAzureUsers
    {
        [FunctionName("HttpGetAzureUsers")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, nameof(HttpMethods.Get), Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            try
            {
                var config = new ZzootaConfigurationService();
                var zzootaService = new ZzootaUserService(log, config);
                await zzootaService.UserList(DateTime.Now, DateTime.Now.AddHours(-1));

                return new JsonResult(zzootaService.Result.Users);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new OkObjectResult("Http trigger failed: " + ex.Message);
            }
        }
    }
}
```

```
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Optix.Services.Shared.Services;
using Optix.Services.Shared.Services.GraphService;
using Optix.Services.Zoota.Core;

namespace Optix.Services.Zoota.Functions
{
    public class TimerGetAzureUsers
    {
        [FunctionName("TimerGetAzureUsers")]
        public async Task Run([TimerTrigger("0 0 */12 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.IsPastDue)
                log.LogInformation("Timer is running late!");


            var config = new ZzootaConfigurationService();

            try
            {
                var zzootaService = new ZzootaUserService(log, config);
                await zzootaService.UserList(DateTime.Now, DateTime.Now.AddHours(-1));

                log.LogInformation("Timer function finished!");
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error: {ex.Message}");
                // Send message with the error
                GraphNotification.SendNotification(config.ZzootaWebHook(), null, ex.Message);
            }
        }
    }
}
```

```
using DriveRisk.Telematics.Shared.Services.GraphService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Optix.Services.Shared.Core;
using Optix.Services.Shared.Models;
using Optix.Services.Shared.Services.GraphService;

namespace Optix.Services.Shared.Services
{
    public class ZzootaUserService
    {
        public string AzureConnectionString { get; set; }
        public SaveModel? Result = new();

        private readonly string[] _all = GraphFields.All;
        private readonly IZzootaConfigurationService _config;
        private readonly IAzureUserFetcher? _graph;
        private readonly ILogger? _logger;

        public ZzootaUserService(ILogger? logger, IZzootaConfigurationService config)
        {
            _logger = logger;
            _config = config;

            AzureConnectionString = _config.ZzootaConnectionString();

            _graph = GraphContext.FetchADUsers(
                GraphContext.CreateADAuthentication(config)
            );
        }

        public async Task UserList(DateTime startDate, DateTime endDate)
        {
            /// <summary>
            /// The ConfigureAwait(false) method is called on the returned Task to indicate that the method does not require the context of the calling thread to resume.
            /// This can improve performance in certain scenarios.
            /// Graph API returns all fields
            /// </summary>

            var graph = await _graph!
                .FetchAzureUsers(_all)
                .ConfigureAwait(false);

            Result!.Users = graph.Users!.Select(d => new EmployeeModel
            {
                PreferredLanguage = d.PreferredLanguage,
                Mail = d.Mail ?? d.UserPrincipalName,
                AccountEnabled = d.AccountEnabled,
                OfficeLocation = d.OfficeLocation,
                BusinessPhones = d.BusinessPhones,
                CreatedDate = d.CreatedDateTime,
                EmployeeType = d.EmployeeType,
                MobilePhone = d.MobilePhone,
                CompanyName = d.CompanyName,
                DisplayName = d.DisplayName,
                EmployeeId = d.EmployeeId,
                Identities = d.Identities,
                Activities = d.Activities,
                Department = d.Department,
                GivenName = d.GivenName,
                JobTitle = d.JobTitle,
                Surname = d.Surname,
                Manager = d.Manager,
                Guid = d.Id
            })
           .ToList();

            Result!.RequestUrl = graph.RequestUrl;

            Save(Result, startDate, endDate);
        }

        public void Save(SaveModel? model, DateTime startDate, DateTime endDate)
        {
            var dc = new DriveRiskDriverDisptachContext(AzureConnectionString);

            var result = JsonConvert.SerializeObject(model!.Users);

            var graphResponse = dc.TR_AzureUserInsert(result);

            //var result = dc.Zzoota_AzureRequestInsert(
            //    JsonConvert.SerializeObject(model!.Users), // Convert result into Json
            //    model.RequestUrl,
            //    startDate,
            //    endDate
            //);

            GraphNotification.SendNotification(
               _config.ZzootaWebHook(),
               graphResponse.SingleOrDefault(),
               null
            );
        }
    }
}
```

```
using Microsoft.Graph;
using Azure.Identity;

namespace Optix.Services.Shared.Services.GraphService
{
    // An abstract class that can be modified through the class it's being inherited from
    public interface IGraphAuthenticator
    {
        GraphServiceClient Create();
    }

    public class GraphAuthenticator : IGraphAuthenticator
    {
        private readonly string? _clientId;
        private readonly string? _tenantId;
        private readonly string? _clientSecret;

        public GraphAuthenticator(string? clientId, string? tenantId, string? clientSecret)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _clientSecret = clientSecret;
        }

        // https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider
        // Creates a autherized connection to Azure Active Directory
        // using a Client Id, Tenant Id and Client Secret that was generated in App Registration.
        public GraphServiceClient Create()
        {
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // Create the Auth Provider
            // The client credential flow enables service applications to run without user interaction.
            var authProvider = new ClientSecretCredential(_tenantId, _clientId, _clientSecret, options);

            // Create Graph Service Client
            GraphServiceClient graphClient = new(authProvider);

            return graphClient;
        }
    }
}
```

```
using Optix.Services.Shared.Core;
using Optix.Services.Shared.Services.GraphService;

namespace DriveRisk.Telematics.Shared.Services.GraphService
{
    public class GraphContext
    {
        public static GraphAuthenticator CreateADAuthentication(IZzootaConfigurationService config)
        {
            return new GraphAuthenticator(
                config.ZzootaClientId(),
                config.ZzootaTenantId(),
                config.ZzootaClientSecret()
            );
        }

        public static IAzureUserFetcher FetchADUsers(IGraphAuthenticator authenticator)
        {
            return new GraphUserFetcher(authenticator);
        }
    }
}
```

```
﻿using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Optix.Services.Shared.Models;
using System.Text;

namespace Optix.Services.Shared.Services.GraphService
{
    public class GraphNotification
    {
        public static async void SendNotification(string webHookUrl, TR_AzureUserInsertResult? payloadData, string? errorMessage)
        {
            // Resource https://leovoel.github.io/embed-visualizer/
            int? count = payloadData?.ResultCount;
            string? emails = payloadData?.ResultMessage;

            var payload = new
            {
                embeds = new[]
                {
                    new
                    {

                        title = errorMessage.IsNullOrEmpty() ? "Emails from Azure AD" : "An error occured",
                        description = errorMessage.IsNullOrEmpty() ? count + " new email(s) added. \n\n " + emails : errorMessage,
                        color = 14300042
                    }
                }
            };

            // Convert the payload array to JSON format
            var payloadJson = JsonConvert.SerializeObject(payload);

            // Send the payload to the Discord webhook URL
            var httpClient = new HttpClient();

            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(webHookUrl),
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };

            await httpClient.SendAsync(request);
        }
    }
}
```

```
using Optix.Services.Shared.Models;
using Microsoft.Graph.Models;
using Microsoft.Graph;

namespace Optix.Services.Shared.Services.GraphService
{
    // An abstract class that can be modified through the class it's being inherited from
    public interface IAzureUserFetcher
    {
        Task<GraphModel> FetchAzureUsers(string[] selectFields);
    }

    //A class that implements IAzureUserFetcher and retrieves users from Azure AD using the Microsoft Graph API.
    public class GraphUserFetcher : IAzureUserFetcher
    {
        private readonly GraphServiceClient _graphClient;
        private readonly List<Microsoft.Graph.Models.User>? _usersList = new();
        private readonly GraphModel? _request = new();

        public GraphUserFetcher(IGraphAuthenticator authenticator)
        {
            _graphClient = authenticator.Create();
        }

        public async Task<GraphModel> FetchAzureUsers(string[] selectFields)
        {
            // Resource: https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/dev/docs/upgrade-to-v5.md#query-parameter-options

            var azureUsers = await _graphClient.Users.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Select = selectFields;
                requestConfiguration.QueryParameters.Top = 999;
            });

            var pageIterator = PageIterator<Microsoft.Graph.Models.User, UserCollectionResponse>.CreatePageIterator(
                _graphClient,
                azureUsers!,
                (user) =>
                {
                    _usersList!.Add(user);
                    return true;
                }
            );

            await pageIterator.IterateAsync();

            _request!.RequestUrl = azureUsers!.OdataNextLink;
            _request!.Users = _usersList;

            return _request;
        }
    }
}
```
