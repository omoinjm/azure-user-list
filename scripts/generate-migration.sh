#!/bin/bash

# Database Migration Script for Azure Entra
# Usage: ./generate-migration.sh [migration-name] [database-provider]
# Example: ./generate-migration.sh InitialCreate PostgreSQL
# Example: ./generate-migration.sh AddUserAuditTrail SqlServer

set -e

MIGRATION_NAME=${1:-"InitialCreate"}
DATABASE_PROVIDER=${2:-"PostgreSQL"}

echo "🔧 Azure Entra Database Migration"
echo "=================================="
echo "Migration Name: $MIGRATION_NAME"
echo "Database Provider: $DATABASE_PROVIDER"
echo ""

# Navigate to Infrastructure project
cd src/Infrastructure

echo "📦 Installing EF Core tools..."
dotnet tool install --global dotnet-ef --ignore-failed-sources 2>/dev/null || true

echo "🔄 Creating migration..."

# Determine context based on database provider
if [[ "$DATABASE_PROVIDER" =~ ^[Pp]ostgre[SQLsql]*$ ]]; then
    CONTEXT="PostgreSqlAzureEntraDbContext"
    echo "🐘 Using PostgreSQL context: $CONTEXT"
elif [[ "$DATABASE_PROVIDER" =~ ^[Ss][Qq][Ll]$|^SqlServer$|^[Mm][Ss][Ss][Qq][Ll]$ ]]; then
    CONTEXT="SqlServerAzureEntraDbContext"
    echo "🗄️  Using SQL Server context: $CONTEXT"
else
    echo "❌ Error: Invalid database provider. Must be PostgreSQL or SqlServer"
    exit 1
fi

# Create migration
dotnet ef migrations add "$MIGRATION_NAME" \
  --startup-project ../AzureEntra.Functions \
  --context "$CONTEXT" \
  --no-build

echo "✅ Migration '$MIGRATION_NAME' created successfully for $DATABASE_PROVIDER!"

echo ""
echo "📝 Generating SQL script..."
# Generate idempotent SQL script
dotnet ef migrations script \
  --startup-project ../AzureEntra.Functions \
  --context "$CONTEXT" \
  --output "migration_${MIGRATION_NAME}_${DATABASE_PROVIDER}.sql" \
  --idempotent \
  --no-build

echo ""
echo "✅ Migration and SQL script generated successfully!"
echo ""
echo "📄 Files created:"
echo "  - Migrations/$(date +%Y%m%d%H%M%S)_${MIGRATION_NAME}.cs"
echo "  - migration_${MIGRATION_NAME}_${DATABASE_PROVIDER}.sql"
echo ""
echo "📋 Next steps:"
echo "  1. Review the generated migration in Migrations/ folder"
echo "  2. Review the SQL script: migration_${MIGRATION_NAME}_${DATABASE_PROVIDER}.sql"
echo "  3. For staging: Run the SQL script against your staging database"
echo "  4. For production: Follow your production deployment process"
echo ""
echo "💾 To apply locally:"
echo "  ./scripts/apply-migration.sh Development $DATABASE_PROVIDER"
echo ""