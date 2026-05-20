using Microsoft.EntityFrameworkCore;
using Pollon.Publication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pollon.Content.Api.Data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{

    public DbSet<PublishedContent> PublishedContents => Set<PublishedContent>();
    public DbSet<ContentSubmission> ContentSubmissions => Set<ContentSubmission>();
    public DbSet<ContentTemplate> ContentTemplates => Set<ContentTemplate>();

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

        var submission = modelBuilder.Entity<ContentSubmission>();
        submission.ToTable("contentsubmissions");
        submission.HasKey(x => x.Id);
        submission.Property(x => x.JsonData).HasColumnType("jsonb");
        submission.HasIndex(x => x.ContentItemId);

        var template = modelBuilder.Entity<ContentTemplate>();
        template.ToTable("contenttemplates");
        template.HasKey(x => x.Id);

        var variablesComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, string>>(
            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
            c => c.Aggregate(0, (a, y) => HashCode.Combine(a, y.Key.GetHashCode(), y.Value.GetHashCode())),
            c => new Dictionary<string, string>(c)
        );

        template.Property(x => x.Variables)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>(),
                variablesComparer
            );

        var tagsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, y) => HashCode.Combine(a, y.GetHashCode())),
            c => new List<string>(c)
        );

        template.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                tagsComparer
            );
    }
}
