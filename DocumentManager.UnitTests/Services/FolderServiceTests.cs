using FluentAssertions;
using DocumentManager.Infrastructure.Services;
using DocumentManager.Core.Entities;
using DocumentManager.Core.Exceptions;
using DocumentManager.UnitTests.Common;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.UnitTests.Services;

public class FolderServiceTests
{
    [Fact]
    public async Task Should_Create_Folder()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FolderService(context);

        await service.CreateAsync("Root", null, "user");

        var folder = context.Folders.FirstOrDefault(x => x.Name == "Root");

        folder.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Enforce_Unique_Name_In_Same_Parent()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FolderService(context);

        var parentId = Guid.NewGuid();

        context.Folders.Add(new Folder
        {
            Id = parentId,
            Name = "Parent",
            CreatedBy = "user",
            CreatedAt = DateTime.UtcNow
        });

        context.Folders.Add(new Folder
        {
            Name = "Docs",
            ParentFolderId = parentId,
            CreatedBy = "user",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        Func<Task> act = async () =>
            await service.CreateAsync("Docs", parentId, "user");

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Should_Not_Allow_Delete_Non_Empty_Folder()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FolderService(context);

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            CreatedBy = "user",
            CreatedAt = DateTime.UtcNow
        };

        context.Folders.Add(folder);

        context.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folder.Id,
            Name = "file.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        Func<Task> act = async () =>
            await service.DeleteAsync(folder.Id);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Should_Enforce_Max_Depth_10()
    {
        using var context = TestDbContextFactory.Create();
        var service = new FolderService(context);

        Guid? parentId = null;

        for (int i = 0; i < 11; i++)
        {
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                Name = $"F{i}",
                ParentFolderId = parentId,
                CreatedBy = "user",
                CreatedAt = DateTime.UtcNow
            };

            context.Folders.Add(folder);
            await context.SaveChangesAsync();

            parentId = folder.Id;
        }

        Func<Task> act = async () =>
            await service.CreateAsync("TooDeep", parentId, "user");

        await act.Should().ThrowAsync<Exception>();
    }
}