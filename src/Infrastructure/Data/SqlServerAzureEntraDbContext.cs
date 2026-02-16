using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    /// <summary>
    /// SQL Server-specific DbContext for the Azure Entra user data.
    /// </summary>
    public class SqlServerAzureEntraDbContext : AzureEntraDbContext
    {
        public SqlServerAzureEntraDbContext(DbContextOptions<SqlServerAzureEntraDbContext> options) 
            : base(options)
        {
        }
    }
}