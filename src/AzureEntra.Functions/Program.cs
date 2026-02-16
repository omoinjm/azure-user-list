using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Core.Configuration;
using Infrastructure.Graph;
using Infrastructure.Persistence;
using Infrastructure.Health;
using Infrastructure.Data;
using Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Get the database provider from configuration to determine which DB context to use
var configuration = new AzureEntraConfiguration();
var databaseProvider = configuration.DatabaseProvider?.ToLowerInvariant() ?? "postgresql";

// Register services for dependency injection
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    // Configuration
    .AddSingleton<IAzureEntraConfiguration, AzureEntraConfiguration>()
    // Graph services
    .AddScoped<IGraphAuthenticator, GraphAuthenticator>()
    .AddScoped<IAzureEntraUserFetcher, GraphUserFetcher>()
    // Persistence services
    .AddScoped<UserRepositoryFactory>()
    .AddScoped<IAzureEntraUserRepository>(serviceProvider =>
    {
        var factory = serviceProvider.GetRequiredService<UserRepositoryFactory>();
        return factory.CreateRepository();
    })
    .AddScoped<AzureEntraDbContext>(serviceProvider =>
    {
        var config = serviceProvider.GetRequiredService<IAzureEntraConfiguration>();
        var provider = config.DatabaseProvider?.ToLowerInvariant() ?? "postgresql";
        
        switch (provider)
        {
            case "postgresql":
            case "postgres":
                var postgresOptions = new DbContextOptionsBuilder<PostgreSqlAzureEntraDbContext>()
                    .UseNpgsql(config.PostgreSqlConnectionString)
                    .Options;
                return new PostgreSqlAzureEntraDbContext(postgresOptions);
            case "sqlserver":
            case "mssql":
                var sqlOptions = new DbContextOptionsBuilder<SqlServerAzureEntraDbContext>()
                    .UseSqlServer(config.SqlConnectionString)
                    .Options;
                return new SqlServerAzureEntraDbContext(sqlOptions);
            default:
                throw new InvalidOperationException($"Unsupported database provider: {provider}");
        }
    })
    .AddScoped<IDbContextFactory<AzureEntraDbContext>>(serviceProvider =>
    {
        var config = serviceProvider.GetRequiredService<IAzureEntraConfiguration>();
        var provider = config.DatabaseProvider?.ToLowerInvariant() ?? "postgresql";
        
        return new DbContextFactoryWrapper(config, provider);
    })
    .AddScoped<EfUserRepositoryFactory>()
    .AddScoped<IEfAzureEntraUserRepository>(serviceProvider =>
    {
        var dbContext = serviceProvider.GetRequiredService<AzureEntraDbContext>();
        var factory = serviceProvider.GetRequiredService<EfUserRepositoryFactory>();
        return factory.CreateEfRepository(dbContext);
    })
    .AddScoped(provider => new DatabaseHealthCheck(provider))
    // Business services
    .AddScoped<AzureEntraUserService>()
    // Health checks
    .AddHealthChecks()
    .AddCheck<AzureADHealthCheck>("AzureADConnectivity")
    .AddDbContextCheck<AzureEntraDbContext>("Database")
    .AddCheck<ApplicationHealthCheck>("Application")
    .AddCheck<DatabaseHealthCheck>("DatabaseConnectivity");

builder.Build().Run();
