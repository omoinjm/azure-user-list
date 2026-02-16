# Entity Framework Guide for Azure Entra

This document provides a comprehensive overview of how Entity Framework (EF) Core is implemented in the Azure Entra project, covering entity creation, DbContext configuration, and migration management.

## Table of Contents
1. [Overview](#overview)
2. [Entity Models](#entity-models)
3. [DbContext Configuration](#dbcontext-configuration)
4. [Creating Migrations](#creating-migrations)
5. [Applying Migrations](#applying-migrations)
6. [Working with Entities](#working-with-entities)
7. [Troubleshooting](#troubleshooting)

## Overview

Entity Framework Core is used in this project to handle data persistence for Azure Entra user records. The implementation supports both PostgreSQL and SQL Server databases through a flexible architecture that uses abstract base classes and concrete implementations.

## Entity Models

### AzureUserRecord Entity

The primary entity in this project is `AzureUserRecord`, located in `src/Infrastructure/Entities/AzureUserRecord.cs`:

```csharp
using System;
using System.Text.Json;

namespace Infrastructure.Entities
{
    /// <summary>
    /// EF Core entity representing an Azure Entra user record in the database.
    /// This entity stores the user data as JSON along with metadata.
    /// </summary>
    public class AzureUserRecord
    {
        public int Id { get; set; }

        public DateTime QueryDate { get; set; } = DateTime.UtcNow;

        // Store the user data as JSON in the database
        public string QueryResult { get; set; }
    }
}
```

#### Key Features:
- **Id**: Auto-generated primary key
- **QueryDate**: Timestamp of when the query was performed (defaults to UTC now)
- **QueryResult**: JSON string containing the Azure Entra user data

## DbContext Configuration

The project uses a flexible DbContext architecture with an abstract base class and concrete implementations for different database providers.

### Base DbContext

The `AzureEntraDbContext` serves as the base class and is located in `src/Infrastructure/Data/AzureEntraDbContext.cs`:

```csharp
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    /// <summary>
    /// Base DbContext for the Azure Entra user data.
    /// This context can be configured to work with either PostgreSQL or SQL Server.
    /// </summary>
    public abstract class AzureEntraDbContext : DbContext
    {
        protected AzureEntraDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<AzureUserRecord> AzureUserRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the AzureUserRecords entity
            modelBuilder.Entity<AzureUserRecord>(entity =>
            {
                entity.ToTable("tr_azure_user_query"); // Table name in snake_case for PostgreSQL convention

                entity.HasKey(e => e.Id);

                entity.Property(e => e.QueryDate)
                    .HasColumnName("query_date")
                    .HasColumnType("timestamp with time zone");

                entity.Property(e => e.QueryResult)
                    .HasColumnName("query_result")
                    .IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
```

### Concrete Implementations

#### SqlServerAzureEntraDbContext

For SQL Server databases, located in `src/Infrastructure/Data/SqlServerAzureEntraDbContext.cs`:

```csharp
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
```

#### PostgreSqlAzureEntraDbContext

For PostgreSQL databases, located in `src/Infrastructure/Data/PostgreSqlAzureEntraDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    /// <summary>
    /// PostgreSQL-specific DbContext for the Azure Entra user data.
    /// </summary>
    public class PostgreSqlAzureEntraDbContext : AzureEntraDbContext
    {
        public PostgreSqlAzureEntraDbContext(DbContextOptions<PostgreSqlAzureEntraDbContext> options)
            : base(options)
        {
        }
    }
}
```

## Creating Migrations

### Understanding the Migration Process

Entity Framework migrations allow you to evolve your database schema over time as your application changes. Each migration represents a set of changes to your database schema that can be applied or rolled back.

### Manual Migration Creation

To create a new migration manually, use the following command from the project root:

```bash
# For PostgreSQL
dotnet ef migrations add "MigrationName" \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext \
  --output-dir ./src/Infrastructure/Migrations/PostgreSQL

# For SQL Server
dotnet ef migrations add "MigrationName" \
  --startup-project ./src/AzureEntra.Functions \
  --context SqlServerAzureEntraDbContext \
  --output-dir ./src/Infrastructure/Migrations/SqlServer
```

When creating a migration, EF Core will:
1. Compare your current model with the last migration
2. Generate a new migration file with the necessary changes
3. Create both `Up()` and `Down()` methods for applying and reverting changes

### Using the Migration Script

The project includes a convenient script for generating migrations:

```bash
# Usage: ./scripts/generate-migration.sh [migration-name] [database-provider]
./scripts/generate-migration.sh InitialCreate PostgreSQL
# or
./scripts/generate-migration.sh InitialCreate SqlServer
```

This script:
1. Installs the EF Core tools if not present
2. Creates the migration for the specified database provider
3. Generates an idempotent SQL script for manual deployment
4. Provides next steps for applying the migration

### Migration Context Factories

The project includes context factories for both database providers in `src/Infrastructure/Migrations/MigrationContextFactory.cs`:

```csharp
public class PostgreSqlMigrationContextFactory
{
    public PostgreSqlAzureEntraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlAzureEntraDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=placeholder;Username=placeholder;Password=placeholder;");
        return new PostgreSqlAzureEntraDbContext(optionsBuilder.Options);
    }
}

public class SqlServerMigrationContextFactory
{
    public SqlServerAzureEntraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerAzureEntraDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=placeholder;Trusted_Connection=true;");
        return new SqlServerAzureEntraDbContext(optionsBuilder.Options);
    }
}
```

### Migration File Structure

Each migration consists of:
- A C# file with `Up()` and `Down()` methods
- An associated `.Designer.cs` file with metadata
- The `Up()` method applies the migration changes
- The `Down()` method reverts the changes

Example migration file:
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzureEntra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tr_azure_user_query",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QueryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QueryResult = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tr_azure_user_query", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tr_azure_user_query");
        }
    }
}
```

## Applying Migrations

### Understanding Migration Application

When you apply migrations, EF Core executes the `Up()` method of each pending migration in sequence, transforming your database schema to match your current model. The migration history is tracked in the `__EFMigrationsHistory` table.

### Manual Migration Application

To apply migrations manually, use the following command from the project root:

```bash
# For PostgreSQL
dotnet ef database update \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext

# For SQL Server
dotnet ef database update \
  --startup-project ./src/AzureEntra.Functions \
  --context SqlServerAzureEntraDbContext
```

You can also apply a specific migration:

```bash
# Apply up to a specific migration
dotnet ef database update MigrationName \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext
```

### Using the Apply Migration Script

The project includes a script for applying migrations across different environments:

```bash
# Usage: ./scripts/apply-migration.sh [environment] [database-provider]
./scripts/apply-migration.sh Development PostgreSQL
# or
./scripts/apply-migration.sh Staging SqlServer
```

The script handles three environments:
- **Development**: Applies migrations to a local database
- **Staging**: Provides guidance for staging deployments
- **Production**: Includes safety checks and approval prompts

### Migration Rollback

To rollback migrations, you can use:

```bash
# Revert to the previous migration
dotnet ef database update PreviousMigrationName \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext

# Revert all migrations (drops all tables)
dotnet ef database update 0 \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext
```

### Migration Status

Check the status of your migrations:

```bash
# List all migrations and their status
dotnet ef migrations list \
  --startup-project ./src/AzureEntra.Functions \
  --context PostgreSqlAzureEntraDbContext
```

### Production Deployment Considerations

When deploying migrations to production:

1. **Always test migrations** on a copy of production data first
2. **Create database backups** before applying migrations
3. **Schedule maintenance windows** for schema changes that might affect availability
4. **Have a rollback plan** ready in case of issues
5. **Monitor application logs** during and after migration application

## Working with Entities

### Basic CRUD Operations

Here are examples of common operations with the `AzureUserRecord` entity:

#### Adding a Record

```csharp
// In your service or repository
public async Task AddAzureUserRecordAsync(AzureUserRecord record)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    context.AzureUserRecords.Add(record);
    await context.SaveChangesAsync();
}

// Or using a scoped service approach with dependency injection
public class AzureUserRecordService
{
    private readonly PostgreSqlAzureEntraDbContext _context;

    public AzureUserRecordService(PostgreSqlAzureEntraDbContext context)
    {
        _context = context;
    }

    public async Task<int> AddAzureUserRecordAsync(AzureUserRecord record)
    {
        _context.AzureUserRecords.Add(record);
        await _context.SaveChangesAsync();
        return record.Id; // Return the ID of the newly created record
    }
}
```

#### Querying Records

```csharp
// Get all records
public async Task<List<AzureUserRecord>> GetAllAzureUserRecordsAsync()
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords.ToListAsync();
}

// Get records by date range
public async Task<List<AzureUserRecord>> GetAzureUserRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords
        .Where(r => r.QueryDate >= startDate && r.QueryDate <= endDate)
        .ToListAsync();
}

// Get the most recent record
public async Task<AzureUserRecord> GetMostRecentAzureUserRecordAsync()
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords
        .OrderByDescending(r => r.QueryDate)
        .FirstOrDefaultAsync();
}

// Get a specific record by ID
public async Task<AzureUserRecord> GetAzureUserRecordByIdAsync(int id)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords.FindAsync(id);
}

// Count records
public async Task<int> GetAzureUserRecordCountAsync()
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords.CountAsync();
}

// Advanced querying with projection
public async Task<List<(int Id, DateTime QueryDate)>> GetIdAndDateOnlyAsync()
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    return await context.AzureUserRecords
        .Select(r => new { r.Id, r.QueryDate })
        .AsNoTracking() // Use AsNoTracking for read-only queries to improve performance
        .ToListAsync();
}
```

#### Updating a Record

```csharp
public async Task<bool> UpdateAzureUserRecordAsync(AzureUserRecord record)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    var existingRecord = await context.AzureUserRecords.FindAsync(record.Id);
    if (existingRecord == null)
    {
        return false; // Record not found
    }

    // Update properties
    existingRecord.QueryDate = record.QueryDate;
    existingRecord.QueryResult = record.QueryResult;
    
    await context.SaveChangesAsync();
    return true;
}

// Alternative: Update specific properties only
public async Task<bool> UpdateAzureUserRecordQueryResultAsync(int id, string newQueryResult)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    var record = await context.AzureUserRecords.FindAsync(id);
    if (record == null)
    {
        return false;
    }

    record.QueryResult = newQueryResult;
    record.QueryDate = DateTime.UtcNow; // Update timestamp
    
    await context.SaveChangesAsync();
    return true;
}
```

#### Deleting a Record

```csharp
public async Task<bool> DeleteAzureUserRecordAsync(int id)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    var record = await context.AzureUserRecords.FindAsync(id);
    if (record != null)
    {
        context.AzureUserRecords.Remove(record);
        await context.SaveChangesAsync();
        return true;
    }
    return false; // Record not found
}

// Bulk delete by date range
public async Task<int> DeleteAzureUserRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    var recordsToDelete = await context.AzureUserRecords
        .Where(r => r.QueryDate >= startDate && r.QueryDate <= endDate)
        .ToListAsync();
        
    context.AzureUserRecords.RemoveRange(recordsToDelete);
    return await context.SaveChangesAsync();
}
```

### Advanced Entity Operations

#### Async Operations with Proper Exception Handling

```csharp
public async Task<AzureUserRecord> AddAzureUserRecordWithRetryAsync(AzureUserRecord record)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    
    try
    {
        context.AzureUserRecords.Add(record);
        await context.SaveChangesAsync();
        return record;
    }
    catch (DbUpdateException ex)
    {
        // Log the exception
        Console.WriteLine($"Database update error: {ex.Message}");
        throw; // Re-throw or handle as appropriate
    }
    catch (Exception ex)
    {
        // Handle other exceptions
        Console.WriteLine($"General error: {ex.Message}");
        throw;
    }
}
```

#### Using Transactions for Multiple Operations

```csharp
public async Task<bool> PerformMultipleOperationsAsync(List<AzureUserRecord> recordsToAdd, List<int> idsToDelete)
{
    using var context = new PostgreSqlAzureEntraDbContext(options);
    using var transaction = await context.Database.BeginTransactionAsync();
    
    try
    {
        // Add new records
        context.AzureUserRecords.AddRange(recordsToAdd);
        
        // Delete specified records
        var recordsToDelete = await context.AzureUserRecords
            .Where(r => idsToDelete.Contains(r.Id))
            .ToListAsync();
            
        context.AzureUserRecords.RemoveRange(recordsToDelete);
        
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return true;
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        return false;
    }
}
```

#### Using Repository Pattern

For better separation of concerns, consider implementing a repository pattern:

```csharp
public interface IAzureUserRecordRepository
{
    Task<List<AzureUserRecord>> GetAllAsync();
    Task<AzureUserRecord> GetByIdAsync(int id);
    Task<List<AzureUserRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> AddAsync(AzureUserRecord record);
    Task<bool> UpdateAsync(AzureUserRecord record);
    Task<bool> DeleteAsync(int id);
    Task<int> CountAsync();
}

public class AzureUserRecordRepository : IAzureUserRecordRepository
{
    private readonly PostgreSqlAzureEntraDbContext _context;

    public AzureUserRecordRepository(PostgreSqlAzureEntraDbContext context)
    {
        _context = context;
    }

    public async Task<List<AzureUserRecord>> GetAllAsync()
    {
        return await _context.AzureUserRecords.ToListAsync();
    }

    public async Task<AzureUserRecord> GetByIdAsync(int id)
    {
        return await _context.AzureUserRecords.FindAsync(id);
    }

    public async Task<List<AzureUserRecord>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.AzureUserRecords
            .Where(r => r.QueryDate >= startDate && r.QueryDate <= endDate)
            .ToListAsync();
    }

    public async Task<int> AddAsync(AzureUserRecord record)
    {
        _context.AzureUserRecords.Add(record);
        await _context.SaveChangesAsync();
        return record.Id;
    }

    public async Task<bool> UpdateAsync(AzureUserRecord record)
    {
        var existing = await _context.AzureUserRecords.FindAsync(record.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _context.AzureUserRecords.FindAsync(id);
        if (record == null) return false;

        _context.AzureUserRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountAsync()
    {
        return await _context.AzureUserRecords.CountAsync();
    }
}
```

## Troubleshooting

### Common Issues and Solutions

#### Issue: Migration fails due to existing tables
**Symptom**: Migration fails with "table already exists" or "relation already exists" errors
**Solution**: This commonly occurs when the database already contains the tables from a previous migration. Options:
1. Manually mark the migrations as applied in the `__EFMigrationsHistory` table
2. Reset your database (if acceptable)
3. Use `dotnet ef database update --connection="<connection_string>"` with specific connection

#### Issue: Connection string problems
**Symptom**: Cannot connect to the database during migration
**Solution**: Ensure your connection string is properly configured in your environment variables or configuration files.

#### Issue: Missing EF Core tools
**Symptom**: Commands fail with "dotnet ef" not recognized
**Solution**: Install the EF Core tools globally:
```bash
dotnet tool install --global dotnet-ef
```

#### Issue: Model changes not reflected in migrations
**Symptom**: Changes to entity models don't appear in generated migrations
**Solution**: Ensure you're generating migrations from the correct project directory and using the right DbContext.

#### Issue: Concurrent migration execution
**Symptom**: Multiple processes trying to apply migrations simultaneously causing conflicts
**Solution**: Implement application-level locks or use database-level locking mechanisms during migration application.

#### Issue: Large data migrations
**Symptom**: Migrations taking too long or timing out on large datasets
**Solution**: Break large migrations into smaller chunks or perform data transformations in batches:

```csharp
// Example of processing large datasets in batches
public async Task ProcessLargeDatasetInBatchesAsync()
{
    const int batchSize = 1000;
    int processed = 0;
    
    do
    {
        var batch = await context.AzureUserRecords
            .Where(x => !x.Processed)
            .Take(batchSize)
            .ToListAsync();
            
        foreach (var record in batch)
        {
            // Process individual record
            record.Processed = true;
        }
        
        await context.SaveChangesAsync();
        processed = batch.Count;
        
        // Optional: Add delay to prevent overwhelming the database
        await Task.Delay(100);
    }
    while (processed == batchSize);
}
```

#### Issue: Circular dependency in relationships
**Symptom**: Error during model creation due to circular references between entities
**Solution**: Use navigation property attributes to configure relationships explicitly:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<AzureUserRecord>()
        .HasOne(/* principal */)
        .WithMany(/* dependent */)
        .HasForeignKey(/* fk property */)
        .OnDelete(DeleteBehavior.Cascade);  // Specify delete behavior to avoid cycles
}
```

### Performance Tips

#### Use AsNoTracking for Read-Only Queries
For queries that only read data (no updates), use `AsNoTracking()` to improve performance:

```csharp
var records = await context.AzureUserRecords
    .AsNoTracking()  // Improves read performance
    .ToListAsync();
```

#### Implement Proper Indexing
Make sure to add indexes for commonly queried columns in your migrations:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Create table
    migrationBuilder.CreateTable(
        name: "tr_azure_user_query",
        columns: table => new
        {
            // ... columns
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_tr_azure_user_query", x => x.Id);
        });

    // Add index for frequently queried column
    migrationBuilder.CreateIndex(
        name: "IX_tr_azure_user_query_QueryDate",
        table: "tr_azure_user_query",
        column: "query_date");
}
```

#### Use Projection to Select Only Needed Data
Instead of loading entire entities, select only the data you need:

```csharp
// Inefficient - loads entire entities
var records = await context.AzureUserRecords.ToListAsync();

// Efficient - only loads needed properties
var recordData = await context.AzureUserRecords
    .Select(r => new { r.Id, r.QueryDate })
    .ToListAsync();
```

### Debugging EF Core

#### Enable Detailed Logging
To debug EF Core operations, enable detailed logging in your configuration:

```csharp
// In Program.cs or Startup.cs
services.AddDbContext<PostgreSqlAzureEntraDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)  // Log all SQL commands
           .EnableSensitiveDataLogging();  // Include parameter values in logs (dev only!)
});
```

#### View Generated SQL
To see the SQL generated by EF Core, you can use:

```csharp
// For a specific query
var sql = context.AzureUserRecords.ToQueryString();
Console.WriteLine(sql);

// Or use debugging tools to inspect the query
var query = context.AzureUserRecords.Where(r => r.QueryDate > DateTime.Now.AddDays(-7));
var result = await query.ToListAsync(); // Set breakpoint here to inspect the SQL
```

### Migration Best Practices

1. **Always review generated migrations** before applying them to production
2. **Test migrations on a copy of production data** before applying to production
3. **Backup databases** before applying migrations in production
4. **Use environment-specific configurations** for different database connections
5. **Consider the impact of schema changes** on existing data
6. **Write idempotent migration scripts** when possible for easier rollbacks
7. **Keep migrations small and focused** on single responsibilities
8. **Test both Up() and Down() methods** to ensure rollbacks work correctly

### Useful Commands

```bash
# List pending migrations
dotnet ef migrations list --startup-project ./src/AzureEntra.Functions --context PostgreSqlAzureEntraDbContext

# Generate SQL script for specific migration
dotnet ef migrations script --startup-project ./src/AzureEntra.Functions --context PostgreSqlAzureEntraDbContext --output migration.sql

# Remove the last migration (development only)
dotnet ef migrations remove --startup-project ./src/AzureEntra.Functions --context PostgreSqlAzureEntraDbContext

# Get detailed information about a specific migration
dotnet ef migrations script --startup-project ./src/AzureEntra.Functions --context PostgreSqlAzureEntraDbContext --idempotent --output detailed_migration.sql
```

### Additional Resources

- [Official Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Performance Best Practices](https://docs.microsoft.com/en-us/ef/core/performance/performance-best-practices)