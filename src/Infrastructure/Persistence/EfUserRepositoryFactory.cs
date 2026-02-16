using Core.Configuration;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Factory for creating the EF Core user repository.
    /// </summary>
    public class EfUserRepositoryFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public EfUserRepositoryFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates the EF Core repository.
        /// </summary>
        public IEfAzureEntraUserRepository CreateEfRepository(AzureEntraDbContext dbContext)
        {
            return new EfAzureEntraUserRepository(dbContext, _loggerFactory.CreateLogger<EfAzureEntraUserRepository>());
        }
    }
}