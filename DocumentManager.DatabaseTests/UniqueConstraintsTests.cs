using DocumentManager.Core.Entities;
using DocumentManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DocumentManager.DatabaseTests;

public class UniqueConstraintsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private AppDbContext _context;

    public UniqueConstraintsTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Folder_Name_Must_Be_Unique_Within_Parent()
    {
        // батьк папка
        var parent = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Parent",
            ParentFolderId = null,
            CreatedBy = "t",
            CreatedAt = DateTime.UtcNow
        };
        _context.Folders.Add(parent);
        await _context.SaveChangesAsync();

        // 1 доч папка
        var folder1 = new Folder
        {
            Id = Guid.NewGuid(),
            ParentFolderId = parent.Id,
            Name = "Same",
            CreatedBy = "t",
            CreatedAt = DateTime.UtcNow
        };
        // 2 доч папка
        var folder2 = new Folder
        {
            Id = Guid.NewGuid(),
            ParentFolderId = parent.Id,
            Name = "Same",
            CreatedBy = "t",
            CreatedAt = DateTime.UtcNow
        };

        _context.Folders.Add(folder1);
        await _context.SaveChangesAsync();

        _context.Folders.Add(folder2);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Document_Name_Must_Be_Unique_Within_Folder()
    {
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Folder",
            ParentFolderId = null,
            CreatedBy = "t",
            CreatedAt = DateTime.UtcNow
        };
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        var doc1 = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "doc",
            ContentType = "text",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var doc2 = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "doc",
            ContentType = "text",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(doc1);
        await _context.SaveChangesAsync();

        _context.Documents.Add(doc2);
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }
    [Fact]
public async Task Version_Chain_Integrity_After_Updates()
{
    // створ папку
    var folder = new Folder
    {
        Id = Guid.NewGuid(),
        Name = "TestFolder",
        ParentFolderId = null,
        CreatedBy = "t",
        CreatedAt = DateTime.UtcNow
    };
    _context.Folders.Add(folder);
    await _context.SaveChangesAsync();

    // створ дока
    var doc = new Document
    {
        Id = Guid.NewGuid(),
        FolderId = folder.Id,
        Name = "doc.txt",
        ContentType = "text/plain",
        SizeBytes = 100,
        Version = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    _context.Documents.Add(doc);
    await _context.SaveChangesAsync();

    //початк верс док
    var version1 = new DocumentVersion
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        VersionNumber = 1,
        ChangeSummary = "Initial",
        CreatedBy = "t",
        CreatedAt = DateTime.UtcNow
    };
    _context.DocumentVersions.Add(version1);
    await _context.SaveChangesAsync();

    // оновлення дока
    doc.Version = 2;
    doc.UpdatedAt = DateTime.UtcNow;
    var version2 = new DocumentVersion
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        VersionNumber = 2,
        ChangeSummary = "Update 1",
        CreatedBy = "t",
        CreatedAt = DateTime.UtcNow
    };
    _context.DocumentVersions.Add(version2);
    await _context.SaveChangesAsync();

    // версії
    var versions = await _context.DocumentVersions
        .Where(v => v.DocumentId == doc.Id)
        .OrderBy(v => v.VersionNumber)
        .ToListAsync();

    Assert.Equal(2, versions.Count);
    Assert.All(versions, v => Assert.Equal(doc.Id, v.DocumentId));
    Assert.Equal(1, versions[0].VersionNumber);
    Assert.Equal(2, versions[1].VersionNumber);
}

[Fact]
public async Task Recursive_Folder_Tree_Retrieval()
{
    //дерево
    var root = new Folder { Id = Guid.NewGuid(), Name = "Root", ParentFolderId = null, CreatedBy = "t", CreatedAt = DateTime.UtcNow };
    var level1 = new Folder { Id = Guid.NewGuid(), Name = "Level1", ParentFolderId = root.Id, CreatedBy = "t", CreatedAt = DateTime.UtcNow };
    var level2 = new Folder { Id = Guid.NewGuid(), Name = "Level2", ParentFolderId = level1.Id, CreatedBy = "t", CreatedAt = DateTime.UtcNow };
    var level3 = new Folder { Id = Guid.NewGuid(), Name = "Level3", ParentFolderId = level2.Id, CreatedBy = "t", CreatedAt = DateTime.UtcNow };

    _context.Folders.AddRange(root, level1, level2, level3);
    await _context.SaveChangesAsync();

    // завант корн папку з підпапками
    var loadedRoot = await _context.Folders
        .Include(f => f.SubFolders)
        .ThenInclude(f => f.SubFolders)
        .ThenInclude(f => f.SubFolders)
        .FirstOrDefaultAsync(f => f.Id == root.Id);

    Assert.NotNull(loadedRoot);
    Assert.Single(loadedRoot.SubFolders);
    Assert.Equal("Level1", loadedRoot.SubFolders[0].Name);
    Assert.Single(loadedRoot.SubFolders[0].SubFolders);
    Assert.Equal("Level2", loadedRoot.SubFolders[0].SubFolders[0].Name);
    Assert.Single(loadedRoot.SubFolders[0].SubFolders[0].SubFolders);
    Assert.Equal("Level3", loadedRoot.SubFolders[0].SubFolders[0].SubFolders[0].Name);
}
}