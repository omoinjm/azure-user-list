using Core.Configuration;
using Core.Models;
using Infrastructure.Graph;
using Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Services.UnitTests
{
    public class AzureEntraUserServiceTests
    {
        private readonly Mock<IAzureEntraUserFetcher> _mockUserFetcher;
        private readonly Mock<IAzureEntraUserRepository> _mockRepository;
        private readonly Mock<IAzureEntraConfiguration> _mockConfig;
        private readonly Mock<ILogger> _mockLogger;
        private readonly AzureEntraUserService _service;

        public AzureEntraUserServiceTests()
        {
            _mockUserFetcher = new Mock<IAzureEntraUserFetcher>();
            _mockRepository = new Mock<IAzureEntraUserRepository>();
            _mockConfig = new Mock<IAzureEntraConfiguration>();
            _mockLogger = new Mock<ILogger>();

            _service = new AzureEntraUserService(
                _mockUserFetcher.Object,
                _mockRepository.Object,
                _mockConfig.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SynchronizeUsersAsync_WithValidUsers_ReturnsSuccessResult()
        {
            // Arrange
            var users = new List<AzureEntraUser> { new AzureEntraUser { Id = "1", DisplayName = "Test User" } };
            var queryResult = new QueryResult { Users = users };
            
            _mockUserFetcher.Setup(x => x.FetchUsersAsync(It.IsAny<string[]>()))
                .ReturnsAsync(queryResult);
            _mockRepository.Setup(x => x.SaveUsersAsync(It.IsAny<IEnumerable<AzureEntraUser>>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SynchronizeUsersAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.UserCount);
            Assert.Equal(1, result.RowsAffected);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task SynchronizeUsersAsync_WithNoUsers_ReturnsSuccessResultWithZeroCount()
        {
            // Arrange
            var queryResult = new QueryResult { Users = new List<AzureEntraUser>() };
            
            _mockUserFetcher.Setup(x => x.FetchUsersAsync(It.IsAny<string[]>()))
                .ReturnsAsync(queryResult);

            // Act
            var result = await _service.SynchronizeUsersAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.UserCount);
            Assert.Equal(0, result.RowsAffected);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task SynchronizeUsersAsync_WithNullUsers_ReturnsSuccessResultWithZeroCount()
        {
            // Arrange
            var queryResult = new QueryResult { Users = null };
            
            _mockUserFetcher.Setup(x => x.FetchUsersAsync(It.IsAny<string[]>()))
                .ReturnsAsync(queryResult);

            // Act
            var result = await _service.SynchronizeUsersAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, result.UserCount);
            Assert.Equal(0, result.RowsAffected);
            Assert.NotNull(result.Message);
        }

        [Fact]
        public async Task SynchronizeUsersAsync_WithException_ThrowsException()
        {
            // Arrange
            _mockUserFetcher.Setup(x => x.FetchUsersAsync(It.IsAny<string[]>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SynchronizeUsersAsync());
        }
    }
    
    public class PostgreSqlAzureUserRepositoryTests
    {
        [Fact]
        public void Constructor_WithValidConnectionString_DoesNotThrow()
        {
            // Arrange & Act
            var repo = new PostgreSqlAzureUserRepository("Host=localhost;Database=test;Username=user;Password=pass;");
            
            // Assert
            Assert.NotNull(repo);
        }
        
        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAzureUserRepository(null));
        }
        
        [Fact]
        public void Constructor_WithEmptyConnectionString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAzureUserRepository(""));
        }
    }
}