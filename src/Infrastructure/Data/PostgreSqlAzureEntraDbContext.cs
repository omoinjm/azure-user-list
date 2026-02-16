using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    /// <summary>
    /// PostgreSQL-specific DbContext for the Azure Entra user data.
    /// </summary>
    public class PostgreSqlAzureEntraDbContext : AzureEntraDbContext
    {
        public PostgreSqlAzureEntraDbContext(DbContextOptions<PostgreSqlAzureEntraDbContext> options) 
            : base(options)
        {
        }
    }
}