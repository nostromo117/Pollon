using Microsoft.EntityFrameworkCore;
using Pollon.Publication.Models;

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

        // PostgreSQL uses lowercase table names by default
        entity.ToTable("publishedcontents");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.JsonData)
            .HasColumnType("text");

        entity.Property(x => x.HtmlContent)
            .HasColumnType("text");

        entity.Property(x => x.SearchText)
            .HasColumnType("text");

        // Optimize querying by SystemName and Slug
        entity.HasIndex(x => x.SystemName);

        entity.HasIndex(x => x.Slug);
    }
}
