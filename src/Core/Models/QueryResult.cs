using System.Collections.Generic;

namespace Core.Models
{
    /// <summary>
    /// Result container for a Graph API query.
    /// 
    /// WHY: Encapsulates all data returned from user fetch operation,
    /// including pagination information and user list.
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// List of Azure Entra users returned from Graph API.
        /// </summary>
        public List<AzureEntraUser> Users { get; set; } = new();

        /// <summary>
        /// OData next link for pagination.
        /// If present, indicates more pages available.
        /// </summary>
        public string NextLink { get; set; }
    }
}