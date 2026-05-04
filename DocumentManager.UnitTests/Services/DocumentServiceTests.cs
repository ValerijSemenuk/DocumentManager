using FluentAssertions;
using DocumentManager.Infrastructure.Services;
using DocumentManager.Core.Entities;
using DocumentManager.Core.Exceptions;
using DocumentManager.UnitTests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.UnitTests.Services;

public class DocumentServiceTests
{
    [Fact]
    public async Task Should_Create_Document()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "TestFolder",
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var document = await service.CreateAsync(folder.Id, "doc.txt", "text/plain", 100);

        document.Should().NotBeNull();
        document.Version.Should().Be(1);
    }

    [Fact]
    public async Task Should_Reject_File_Larger_Than_50MB()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        Func<Task> act = async () =>
            await service.CreateAsync(folder.Id, "big.bin", "application/octet-stream", 60 * 1024 * 1024);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("File too large");
    }

    [Fact]
    public async Task Should_Increment_Version_On_Update()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 1000,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        await service.UpdateAsync(doc.Id, "updated file", 1200);

        var updated = await context.Documents.FirstAsync(x => x.Id == doc.Id);
        updated.Version.Should().Be(2);
    }

    [Fact]
    public async Task Should_Create_Version_Record_On_Update()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 1000,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        await service.UpdateAsync(doc.Id, "change", 1500);

        var versions = await context.DocumentVersions.Where(v => v.DocumentId == doc.Id).ToListAsync();
        versions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Enforce_Unique_Document_Name_In_Folder()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        context.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "test.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        Func<Task> act = async () =>
            await service.CreateAsync(folder.Id, "test.txt", "text/plain", 100);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("Document name must be unique in folder");
    }

    [Fact]
    public async Task Should_Update_UpdatedAt_On_Modification()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 1000,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        await service.UpdateAsync(doc.Id, "new content", 1200);

        var updated = await context.Documents.FirstAsync(x => x.Id == doc.Id);
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    public async Task Should_Return_Versions_In_Order()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 1000,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Documents.Add(doc);
        await context.SaveChangesAsync();

        await service.UpdateAsync(doc.Id, "v2", 1100);
        await service.UpdateAsync(doc.Id, "v3", 1200);

        var versions = await context.DocumentVersions
            .Where(v => v.DocumentId == doc.Id)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();

        versions.First().VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task Should_Throw_If_Folder_Not_Exists()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        Func<Task> act = async () =>
            await service.CreateAsync(Guid.NewGuid(), "file.txt", "text", 100);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("Folder does not exist");
    }

    [Fact]
    public async Task Should_Not_Allow_Duplicate_Name_On_Update()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId, Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var doc1 = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            Name = "file1.txt",
            ContentType = "text",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var doc2 = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            Name = "file2.txt",
            ContentType = "text",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Documents.AddRange(doc1, doc2);
        await context.SaveChangesAsync();

        Func<Task> act = async () =>
            await service.UpdateAsync(doc2.Id, "file1.txt", 200);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("Document name must be unique in folder");
    }

    [Fact]
    public async Task Should_Search_By_Name()
    {
        using var context = TestDbContextFactory.Create();
        var service = new DocumentService(context);

        var folder = new Folder { Id = Guid.NewGuid(), Name = "Folder", CreatedBy = "test", CreatedAt = DateTime.UtcNow };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        context.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "report.pdf",
            ContentType = "pdf",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.SearchByNameAsync("report");
        result.Should().HaveCount(1);
    }
}