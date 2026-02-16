#!/bin/bash

# Database Migration Application Script for Azure Entra
# Applies pending migrations to the database
# Usage: ./apply-migration.sh [environment] [database-provider] [apply-latest-only]
# Example: ./apply-migration.sh Development PostgreSQL
# Example: ./apply-migration.sh Development PostgreSQL true  # Apply only the latest migration

set -e

ENVIRONMENT=${1:-"Development"}
DATABASE_PROVIDER=${2:-"PostgreSQL"}
APPLY_LATEST_ONLY=${3:-"false"}

echo "🚀 Azure Entra - Apply Database Migration"
echo "=========================================="
echo "Environment: $ENVIRONMENT"
echo "Database Provider: $DATABASE_PROVIDER"
echo ""

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(Development|Staging|Production)$ ]]; then
  echo "❌ Error: Invalid environment. Must be Development, Staging, or Production"
  exit 1
fi

# Validate database provider
if [[ ! "$DATABASE_PROVIDER" =~ ^[Pp]ostgre[SQLsql]*$ ]] && [[ ! "$DATABASE_PROVIDER" =~ ^[Ss][Qq][Ll]$|^SqlServer$|^[Mm][Ss][Ss][Qq][Ll]$ ]]; then
  echo "❌ Error: Invalid database provider. Must be PostgreSQL or SqlServer"
  exit 1
fi

# Navigate to Infrastructure project
cd src/Infrastructure

echo "📦 Installing EF Core tools..."
dotnet tool install --global dotnet-ef --ignore-failed-sources 2>/dev/null || true

# Determine context based on database provider
if [[ "$DATABASE_PROVIDER" =~ ^[Pp]ostgre[SQLsql]*$ ]]; then
    CONTEXT="PostgreSqlAzureEntraDbContext"
    echo "🐘 Using PostgreSQL context: $CONTEXT"
elif [[ "$DATABASE_PROVIDER" =~ ^[Ss][Qq][Ll]$|^SqlServer$|^[Mm][Ss][Ss][Qq][Ll]$ ]]; then
    CONTEXT="SqlServerAzureEntraDbContext"
    echo "🗄️  Using SQL Server context: $CONTEXT"
fi

echo ""
echo "📋 Checking for pending migrations..."
ALL_MIGRATIONS=$(dotnet ef migrations list --startup-project ../AzureEntra.Functions --context "$CONTEXT" 2>&1)

# Check if there are any pending migrations
if echo "$ALL_MIGRATIONS" | grep -q "Pending"; then
  echo "📝 Pending migrations detected. Preparing to apply..."

  # Extract and show the latest pending migration
  LATEST_PENDING=$(echo "$ALL_MIGRATIONS" | grep "Pending" | tail -1 | awk '{print $1}')
  if [ -n "$LATEST_PENDING" ]; then
    echo "🔄 Latest pending migration: $LATEST_PENDING"
  fi
  echo ""
else
  echo "✅ No pending migrations found. Database is up to date!"
  echo "$ALL_MIGRATIONS"
  exit 0
fi

# For different environments
if [ "$ENVIRONMENT" = "Development" ]; then
  echo "💻 Development Environment"
  echo "Applying migrations to local database..."
  echo ""

  # Apply migrations with error handling for existing tables
  if [ "$APPLY_LATEST_ONLY" = "true" ] && [ -n "$LATEST_PENDING" ]; then
    echo "🎯 Applying only the latest migration: $LATEST_PENDING"
    if
      ! dotnet ef database update "$LATEST_PENDING" \
        --startup-project ../AzureEntra.Functions \
        --context "$CONTEXT" 2>&1 | grep -v "Error while accessing the Microsoft.Extensions.Hosting\|Some services are not able to be constructed\|Unable to resolve service for type" \
        >/tmp/migration_output.log
    then
      # Check if the error is due to existing tables
      if grep -q "already exists\|relation.*already exists" /tmp/migration_output.log; then
        echo "⚠️  Warning: Some tables already exist. This may be expected if migrating an existing database."
        echo "ℹ️  This commonly happens when the database already contains the tables from a previous migration."
        echo "ℹ️  To resolve this, you may need to:"
        echo "    1. Manually mark the migrations as applied in the __EFMigrationsHistory table, OR"
        echo "    2. Reset your database (if acceptable), OR"
        echo "    3. Use 'dotnet ef database update --connection=\"<connection_string>\"' with specific connection"

        # Show only the important error info, not the full stack trace
        echo ""
        echo "🔍 Important error details:"
        grep -E "(relation.*already exists|42P07:|PostgresException|CREATE TABLE)" /tmp/migration_output.log | head -10
      else
        echo "❌ Error: Migration failed for reasons other than existing tables."
        cat /tmp/migration_output.log
        exit 1
      fi
    else
      echo ""
      echo "✅ Latest migration applied successfully!"
    fi
  else
    if
      ! dotnet ef database update \
        --startup-project ../AzureEntra.Functions \
        --context "$CONTEXT" 2>&1 | grep -v "Error while accessing the Microsoft.Extensions.Hosting\|Some services are not able to be constructed\|Unable to resolve service for type" \
        >/tmp/migration_output.log
    then
      # Check if the error is due to existing tables
      if grep -q "already exists\|relation.*already exists" /tmp/migration_output.log; then
        echo "⚠️  Warning: Some tables already exist. This may be expected if migrating an existing database."
        echo "ℹ️  This commonly happens when the database already contains the tables from a previous migration."
        echo "ℹ️  To resolve this, you may need to:"
        echo "    1. Manually mark the migrations as applied in the __EFMigrationsHistory table, OR"
        echo "    2. Reset your database (if acceptable), OR"
        echo "    3. Use 'dotnet ef database update --connection=\"<connection_string>\"' with specific connection"

        # Show only the important error info, not the full stack trace
        echo ""
        echo "🔍 Important error details:"
        grep -E "(relation.*already exists|42P07:|PostgresException|CREATE TABLE)" /tmp/migration_output.log | head -10
      else
        echo "❌ Error: Migration failed for reasons other than existing tables."
        cat /tmp/migration_output.log
        exit 1
      fi
    else
      echo ""
      echo "✅ Migrations applied successfully!"
    fi
  fi

  echo ""
  echo "📊 Verify tables were created:"
  if [[ "$DATABASE_PROVIDER" =~ ^[Pp]ostgre[SQLsql]*$ ]]; then
    echo "  psql azure_entra_dev -c \"\\dt\""
  else
    echo "  sqlcmd -S localhost -d azure_entra_dev -Q \"SELECT * FROM INFORMATION_SCHEMA.TABLES\""
  fi
  echo ""

elif [ "$ENVIRONMENT" = "Staging" ]; then
  echo "🟡 Staging Environment"
  echo ""
  echo "⚠️  Manual verification required for staging"
  echo ""
  echo "Steps:"
  echo "  1. Review generated SQL script: migration_*.sql"
  echo "  2. Test against staging database backup first"
  echo "  3. Then apply with connection string:"
  echo "     dotnet ef database update --connection=\"<connection-string>\" \\"
  echo "       --startup-project ../AzureEntra.Functions --context $CONTEXT"
  echo ""

elif [ "$ENVIRONMENT" = "Production" ]; then
  echo "🔴 Production Environment"
  echo ""
  echo "⚠️  CRITICAL: Production migration requires approval"
  echo ""
  echo "Steps:"
  echo "  1. Download migration artifact from GitHub Actions"
  echo "  2. Have DBA review the SQL script"
  echo "  3. Create backup of production database"
  echo "  4. Test rollback procedure"
  echo "  5. Apply migration during maintenance window:"
  echo "     dotnet ef database update --connection=\"<connection-string>\" \\"
  echo "       --startup-project ../AzureEntra.Functions --context $CONTEXT"
  echo ""
  read -p "Continue with production migration? (yes/no): " CONFIRM
  if [ "$CONFIRM" != "yes" ]; then
    echo "❌ Migration cancelled"
    exit 1
  fi

  # Apply migrations with error handling for existing tables
  if [ "$APPLY_LATEST_ONLY" = "true" ] && [ -n "$LATEST_PENDING" ]; then
    echo "🎯 Applying only the latest migration: $LATEST_PENDING"
    if
      ! dotnet ef database update "$LATEST_PENDING" \
        --startup-project ../AzureEntra.Functions \
        --context "$CONTEXT" 2>&1 | grep -v "Error while accessing the Microsoft.Extensions.Hosting\|Some services are not able to be constructed\|Unable to resolve service for type" \
        >/tmp/migration_output.log
    then
      # Check if the error is due to existing tables
      if grep -q "already exists\|relation.*already exists" /tmp/migration_output.log; then
        echo "⚠️  Warning: Some tables already exist. This may be expected if migrating an existing database."
        echo "ℹ️  This commonly happens when the database already contains the tables from a previous migration."
        echo "ℹ️  To resolve this, you may need to:"
        echo "    1. Manually mark the migrations as applied in the __EFMigrationsHistory table, OR"
        echo "    2. Verify the migration sequence is correct"

        # Show only the important error info, not the full stack trace
        echo ""
        echo "🔍 Important error details:"
        grep -E "(relation.*already exists|42P07:|PostgresException|CREATE TABLE)" /tmp/migration_output.log | head -10
      else
        echo "❌ Error: Migration failed for reasons other than existing tables."
        cat /tmp/migration_output.log
        exit 1
      fi
    else
      echo ""
      echo "✅ Latest production migration applied successfully!"
    fi
  else
    if
      ! dotnet ef database update \
        --startup-project ../AzureEntra.Functions \
        --context "$CONTEXT" 2>&1 | grep -v "Error while accessing the Microsoft.Extensions.Hosting\|Some services are not able to be constructed\|Unable to resolve service for type" \
        >/tmp/migration_output.log
    then
      # Check if the error is due to existing tables
      if grep -q "already exists\|relation.*already exists" /tmp/migration_output.log; then
        echo "⚠️  Warning: Some tables already exist. This may be expected if migrating an existing database."
        echo "ℹ️  This commonly happens when the database already contains the tables from a previous migration."
        echo "ℹ️  To resolve this, you may need to:"
        echo "    1. Manually mark the migrations as applied in the __EFMigrationsHistory table, OR"
        echo "    2. Verify the migration sequence is correct"

        # Show only the important error info, not the full stack trace
        echo ""
        echo "🔍 Important error details:"
        grep -E "(relation.*already exists|42P07:|PostgresException|CREATE TABLE)" /tmp/migration_output.log | head -10
      else
        echo "❌ Error: Migration failed for reasons other than existing tables."
        cat /tmp/migration_output.log
        exit 1
      fi
    else
      echo ""
      echo "✅ Production migrations applied successfully!"
    fi
  fi

  echo ""
fi

echo "📚 For more information, see docs/MIGRATIONS.md"