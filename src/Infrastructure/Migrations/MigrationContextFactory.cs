using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Migrations
{
    public class PostgreSqlMigrationContextFactory
    {
        public PostgreSqlAzureEntraDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlAzureEntraDbContext>();
            // This would typically come from configuration, but for migration purposes
            // we'll use a placeholder connection string
            optionsBuilder.UseNpgsql("Host=localhost;Database=placeholder;Username=placeholder;Password=placeholder;");
            
            return new PostgreSqlAzureEntraDbContext(optionsBuilder.Options);
        }
    }
    
    public class SqlServerMigrationContextFactory
    {
        public SqlServerAzureEntraDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqlServerAzureEntraDbContext>();
            // This would typically come from configuration, but for migration purposes
            // we'll use a placeholder connection string
            optionsBuilder.UseSqlServer("Server=localhost;Database=placeholder;Trusted_Connection=true;");
            
            return new SqlServerAzureEntraDbContext(optionsBuilder.Options);
        }
    }
}