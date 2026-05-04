using DocumentManager.Core.Entities;
using DocumentManager.Core.Exceptions;
using DocumentManager.Core.Interfaces;
using DocumentManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;

    public DocumentService(AppDbContext context)
    {
        _context = context;
    }

public async Task<Document> CreateAsync(Guid folderId, string name, string contentType, long sizeBytes)
{
    // чи існ папка
    var folderExists = await _context.Folders.AnyAsync(f => f.Id == folderId);
    if (!folderExists)
        throw new BusinessException("Folder does not exist");

    if (sizeBytes > 50 * 1024 * 1024)
        throw new BusinessException("File too large");

    var exists = await _context.Documents.AnyAsync(d =>
        d.FolderId == folderId && d.Name == name);

    if (exists)
        throw new BusinessException("Document name must be unique in folder");

    var doc = new Document
    {
        Id = Guid.NewGuid(),
        FolderId = folderId,
        Name = name,
        ContentType = contentType ?? "text/plain",
        SizeBytes = sizeBytes,
        Version = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.Documents.Add(doc);

    _context.DocumentVersions.Add(new DocumentVersion
    {
        Id = Guid.NewGuid(),
        DocumentId = doc.Id,
        VersionNumber = 1,
        ChangeSummary = "Initial version",
        CreatedBy = "system",
        CreatedAt = DateTime.UtcNow
    });

    await _context.SaveChangesAsync();
    return doc;
}
    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Document>> SearchByNameAsync(string name)
    {
        return await _context.Documents
            .Where(d => d.Name.ToLower().Contains(name.ToLower()))
            .ToListAsync();
    }

    public async Task<Document> UpdateAsync(Guid id, string name, long sizeBytes)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(x => x.Id == id);

        if (doc == null)
            throw new BusinessException("Document not found");

        if (sizeBytes > 50 * 1024 * 1024)
            throw new BusinessException("File too large");

        var duplicate = await _context.Documents.AnyAsync(d =>
            d.FolderId == doc.FolderId &&
            d.Name == name &&
            d.Id != id);

        if (duplicate)
            throw new BusinessException("Document name must be unique in folder");

        doc.Name = name;
        doc.SizeBytes = sizeBytes;
        doc.Version++;
        doc.UpdatedAt = DateTime.UtcNow;

        _context.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            VersionNumber = doc.Version,
            ChangeSummary = "Updated",
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return doc;
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);

        if (doc == null)
            throw new BusinessException("Document not found");

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentVersion>> GetVersionsAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
}