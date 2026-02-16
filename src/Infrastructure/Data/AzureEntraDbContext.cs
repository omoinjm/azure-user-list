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