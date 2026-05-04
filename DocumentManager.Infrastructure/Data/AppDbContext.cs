using DocumentManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Folder>()
            .HasIndex(f => new { f.Name, f.ParentFolderId })
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.Name, d.FolderId })
            .IsUnique();

        modelBuilder.Entity<Folder>()
            .HasOne(f => f.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}