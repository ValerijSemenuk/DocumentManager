using DocumentManager.Core.Entities;
using DocumentManager.Core.Interfaces;
using DocumentManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DocumentManager.Core.Exceptions;

namespace DocumentManager.Infrastructure.Services;

public class FolderService : IFolderService
{
    private readonly AppDbContext _context;

    public FolderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Folder>> GetRootFoldersAsync()
    {
        return await _context.Folders
            .Where(f => f.ParentFolderId == null)
            .ToListAsync();
    }

    public async Task<Folder?> GetByIdAsync(Guid id)
    {
        return await _context.Folders
            .Include(f => f.SubFolders)
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Folder> CreateAsync(string name, Guid? parentId, string createdBy)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new BusinessException("Name required");

    var exists = await _context.Folders
        .AnyAsync(f => f.ParentFolderId == parentId && f.Name == name);

if (exists)
    throw new InvalidOperationException("Folder name must be unique within parent");

    int depth = 0;
    var currentId = parentId;

    while (currentId != null)
    {
        var parent = await _context.Folders
            .Where(f => f.Id == currentId)
            .Select(f => new { f.ParentFolderId })
            .FirstOrDefaultAsync();

        if (parent == null)
            break;

        depth++;

        if (depth >= 10)
    throw new InvalidOperationException("Max folder depth exceeded");

        currentId = parent.ParentFolderId;
    }

    var folder = new Folder
    {
        Id = Guid.NewGuid(),
        Name = name,
        ParentFolderId = parentId,
        CreatedBy = createdBy ?? "test",
        CreatedAt = DateTime.UtcNow
    };

    _context.Folders.Add(folder);
    await _context.SaveChangesAsync();

    return folder;
}

    public async Task DeleteAsync(Guid id)
    {
        var folder = await _context.Folders
            .Include(f => f.SubFolders)
            .Include(f => f.Documents)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (folder == null)
            throw new BusinessException("Folder not found");

        if (folder.SubFolders.Any() || folder.Documents.Any())
            throw new BusinessException("Folder is not empty");

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();
    }
}