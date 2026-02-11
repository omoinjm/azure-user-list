using Microsoft.Graph;

namespace Infrastructure.Graph
{
    /// <summary>
    /// Contract for creating authenticated Graph Service Clients.
    /// 
    /// WHY: Abstracts Graph authentication details, enables mocking in tests,
    /// supports multiple authentication strategies (client credentials, bearer token, etc.)
    /// </summary>
    public interface IGraphAuthenticator
    {
        /// <summary>
        /// Creates and returns an authenticated GraphServiceClient.
        /// </summary>
        /// <returns>Authenticated GraphServiceClient ready for API calls.</returns>
        GraphServiceClient Create();
    }
}