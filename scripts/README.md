# Azure Entra Migration Scripts

This directory contains scripts for managing database migrations for the Azure Entra project.

## Available Scripts

### `generate-migration.sh`
Creates a new database migration for either PostgreSQL or SQL Server.

**Usage:**
```bash
./scripts/generate-migration.sh [migration-name] [database-provider]
```

**Parameters:**
- `migration-name`: Name of the migration (defaults to "InitialCreate")
- `database-provider`: Target database provider ("PostgreSQL" or "SqlServer", defaults to "PostgreSQL")

**Examples:**
```bash
# Create an initial migration for PostgreSQL
./scripts/generate-migration.sh InitialCreate PostgreSQL

# Create a migration for SQL Server
./scripts/generate-migration.sh AddUserAuditTrail SqlServer

# Create a migration with default provider (PostgreSQL)
./scripts/generate-migration.sh AddUserAuditTrail
```

### `apply-migration.sh`
Applies pending database migrations to the target environment.

**Usage:**
```bash
./scripts/apply-migration.sh [environment] [database-provider] [apply-latest-only]
```

**Parameters:**
- `environment`: Target environment ("Development", "Staging", or "Production", defaults to "Development")
- `database-provider`: Target database provider ("PostgreSQL" or "SqlServer", defaults to "PostgreSQL")
- `apply-latest-only`: Whether to apply only the latest migration ("true" or "false", defaults to "false")

**Examples:**
```bash
# Apply migrations to development environment (PostgreSQL)
./scripts/apply-migration.sh Development PostgreSQL

# Apply migrations to development environment (SQL Server)
./scripts/apply-migration.sh Development SqlServer

# Apply only the latest migration to staging
./scripts/apply-migration.sh Staging PostgreSQL true

# Apply migrations to production (with confirmation)
./scripts/apply-migration.sh Production PostgreSQL
```

## Prerequisites

- .NET 8+ SDK installed
- EF Core tools installed (`dotnet tool install --global dotnet-ef`)
- Appropriate database server running (PostgreSQL or SQL Server)
- Proper connection strings configured in environment

## Migration Process

1. **Generate Migration**: Use `generate-migration.sh` to create a new migration
2. **Review**: Examine the generated migration files and SQL script
3. **Apply**: Use `apply-migration.sh` to apply migrations to your target environment

## Environments

- **Development**: Local development environment
- **Staging**: Pre-production testing environment
- **Production**: Live production environment (requires confirmation)

## Database Providers

- **PostgreSQL**: For PostgreSQL databases
- **SqlServer**: For Microsoft SQL Server databases

## Notes

- Always backup your database before applying migrations in production
- Review generated SQL scripts before applying to production
- The scripts handle common error scenarios and provide guidance for resolution