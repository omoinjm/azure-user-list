using System;
using System.Text.Json;

namespace Infrastructure.Entities
{
    /// <summary>
    /// EF Core entity representing an Azure Entra user record in the database.
    /// This entity stores the user data as JSON along with metadata.
    /// </summary>
    public class AzureUserRecord
    {
        public int Id { get; set; }
        
        public DateTime QueryDate { get; set; } = DateTime.UtcNow;
        
        // Store the user data as JSON in the database
        public string QueryResult { get; set; }
    }
}