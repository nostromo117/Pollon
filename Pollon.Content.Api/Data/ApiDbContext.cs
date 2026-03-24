using Microsoft.EntityFrameworkCore;

using Pollon.Contracts.Models;

namespace Pollon.Content.Api.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
    }

    public DbSet<PublishedContent> PublishedContents => Set<PublishedContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var entity = modelBuilder.Entity<PublishedContent>();
        
        entity.HasKey(x => x.Id);
        
        entity.Property(x => x.JsonData)
            .HasColumnType("nvarchar(max)");

        // Optimize querying by SystemName and Slug
        entity.HasIndex(x => x.SystemName);
            
        entity.HasIndex(x => x.Slug);
    }
}
