using Core.Configuration;
using Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Factory for creating the appropriate user repository based on configuration.
    /// </summary>
    public class UserRepositoryFactory
    {
        private readonly IAzureEntraConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public UserRepositoryFactory(IAzureEntraConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates the appropriate repository based on the configured database provider.
        /// </summary>
        public IAzureEntraUserRepository CreateRepository()
        {
            var provider = _configuration.DatabaseProvider?.ToLowerInvariant() ?? "postgresql";
            
            switch (provider)
            {
                case "postgresql":
                case "postgres":
                    return new PostgreSqlAzureUserRepository(
                        _configuration.PostgreSqlConnectionString,
                        _loggerFactory.CreateLogger<PostgreSqlAzureUserRepository>());
                case "sqlserver":
                case "mssql":
                    return new SqlAzureUserRepository(
                        _configuration.SqlConnectionString,
                        _loggerFactory.CreateLogger<SqlAzureUserRepository>());
                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider: {_configuration.DatabaseProvider}. " +
                        "Supported providers are: PostgreSQL, SqlServer");
            }
        }
    }
}