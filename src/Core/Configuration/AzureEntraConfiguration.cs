using System;

namespace Core.Configuration
{
    /// <summary>
    /// Reads Azure Entra configuration from environment variables.
    /// 
    /// WHY: 
    /// - Centralizes configuration access (not scattered across services)
    /// - Validates all variables at startup (not on first use)
    /// - Enables mocking via IAzureEntraConfiguration interface
    /// - Future: easy to add appsettings.json or KeyVault support
    /// </summary>
    public class AzureEntraConfiguration : IAzureEntraConfiguration
    {
        public string ClientId =>
            GetEnvironmentVariable("AZURE_CLIENT_ID");

        public string TenantId =>
            GetEnvironmentVariable("AZURE_TENANT_ID");

        public string ClientSecret =>
            GetEnvironmentVariable("AZURE_CLIENT_SECRET");

        public string SqlConnectionString =>
            GetEnvironmentVariable("SQL_CONNECTION_STRING");

        public string PostgreSqlConnectionString =>
            GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING");

        public string DatabaseProvider =>
            GetEnvironmentVariableWithDefault("DATABASE_PROVIDER", "PostgreSql");

        /// <summary>
        /// Retrieves environment variable with validation.
        /// Throws InvalidOperationException if not configured.
        /// </summary>
        private static string GetEnvironmentVariable(string name)
        {
            var value = System.Environment.GetEnvironmentVariable(
                name, EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{name}' is not configured. " +
                    $"Please set it before running the function.");
            }

            return value;
        }
        
        /// <summary>
        /// Retrieves environment variable with a default value if not set.
        /// </summary>
        private static string GetEnvironmentVariableWithDefault(string name, string defaultValue)
        {
            var value = System.Environment.GetEnvironmentVariable(
                name, EnvironmentVariableTarget.Process);

            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}