namespace GetAzureADUsers.Core.Configuration
{
    /// <summary>
    /// Contract for Azure AD and application configuration.
    /// 
    /// WHY: Abstracts configuration source, enables mocking in tests,
    /// centralizes configuration access.
    /// </summary>
    public interface IAzureADConfiguration
    {
        /// <summary>
        /// Azure Application (client) ID from App Registration.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Azure Directory (tenant) ID.
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Client secret generated for App Registration.
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// SQL Server connection string for persistence.
        /// </summary>
        string SqlConnectionString { get; }
    }
}
