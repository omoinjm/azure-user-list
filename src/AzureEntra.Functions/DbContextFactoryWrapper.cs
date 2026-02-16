using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Core.Configuration;
using System;

namespace AzureEntra.Functions
{
    public class DbContextFactoryWrapper : IDbContextFactory<AzureEntraDbContext>
    {
        private readonly IAzureEntraConfiguration _configuration;
        private readonly string _provider;

        public DbContextFactoryWrapper(IAzureEntraConfiguration configuration, string provider)
        {
            _configuration = configuration;
            _provider = provider;
        }

        public AzureEntraDbContext CreateDbContext()
        {
            return CreateDbContext(default);
        }

        public AzureEntraDbContext CreateDbContext(CancellationToken cancellationToken)
        {
            switch (_provider.ToLowerInvariant())
            {
                case "postgresql":
                case "postgres":
                    var postgresOptions = new DbContextOptionsBuilder<PostgreSqlAzureEntraDbContext>()
                        .UseNpgsql(_configuration.PostgreSqlConnectionString)
                        .Options;
                    return new PostgreSqlAzureEntraDbContext(postgresOptions);
                case "sqlserver":
                case "mssql":
                    var sqlOptions = new DbContextOptionsBuilder<SqlServerAzureEntraDbContext>()
                        .UseSqlServer(_configuration.SqlConnectionString)
                        .Options;
                    return new SqlServerAzureEntraDbContext(sqlOptions);
                default:
                    throw new InvalidOperationException($"Unsupported database provider: {_provider}");
            }
        }
    }
}