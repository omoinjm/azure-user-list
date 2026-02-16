using Core.Configuration;
using Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Services.UnitTests
{
    public class UserRepositoryFactoryTests
    {
        private readonly Mock<IAzureEntraConfiguration> _mockConfig;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly UserRepositoryFactory _factory;

        public UserRepositoryFactoryTests()
        {
            _mockConfig = new Mock<IAzureEntraConfiguration>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _factory = new UserRepositoryFactory(_mockConfig.Object, _mockLoggerFactory.Object);
        }

        [Fact]
        public void CreateRepository_WithPostgreSqlProvider_ReturnsPostgreSqlRepository()
        {
            // Arrange
            _mockConfig.Setup(x => x.DatabaseProvider).Returns("PostgreSQL");
            _mockConfig.Setup(x => x.PostgreSqlConnectionString).Returns("Host=localhost;Database=test;");

            // Act
            var repository = _factory.CreateRepository();

            // Assert
            Assert.IsType<PostgreSqlAzureUserRepository>(repository);
        }

        [Fact]
        public void CreateRepository_WithPostgresProvider_ReturnsPostgreSqlRepository()
        {
            // Arrange
            _mockConfig.Setup(x => x.DatabaseProvider).Returns("postgres");
            _mockConfig.Setup(x => x.PostgreSqlConnectionString).Returns("Host=localhost;Database=test;");

            // Act
            var repository = _factory.CreateRepository();

            // Assert
            Assert.IsType<PostgreSqlAzureUserRepository>(repository);
        }

        [Fact]
        public void CreateRepository_WithSqlServerProvider_ReturnsSqlRepository()
        {
            // Arrange
            _mockConfig.Setup(x => x.DatabaseProvider).Returns("SqlServer");
            _mockConfig.Setup(x => x.SqlConnectionString).Returns("Server=localhost;Database=test;");

            // Act
            var repository = _factory.CreateRepository();

            // Assert
            Assert.IsType<SqlAzureUserRepository>(repository);
        }

        [Fact]
        public void CreateRepository_WithMssqlProvider_ReturnsSqlRepository()
        {
            // Arrange
            _mockConfig.Setup(x => x.DatabaseProvider).Returns("mssql");
            _mockConfig.Setup(x => x.SqlConnectionString).Returns("Server=localhost;Database=test;");

            // Act
            var repository = _factory.CreateRepository();

            // Assert
            Assert.IsType<SqlAzureUserRepository>(repository);
        }

        [Fact]
        public void CreateRepository_WithInvalidProvider_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockConfig.Setup(x => x.DatabaseProvider).Returns("invalid");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _factory.CreateRepository());
        }
    }
}