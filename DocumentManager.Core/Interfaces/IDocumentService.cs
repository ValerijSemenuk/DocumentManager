using DocumentManager.Core.Entities;

namespace DocumentManager.Core.Interfaces;

public interface IDocumentService
{
    Task<Document> CreateAsync(Guid folderId, string name, string contentType, long sizeBytes);
    Task<Document?> GetByIdAsync(Guid id);
    Task<Document> UpdateAsync(Guid id, string name, long sizeBytes);
    Task DeleteAsync(Guid id);
    Task<List<DocumentVersion>> GetVersionsAsync(Guid documentId);
    Task<List<Document>> SearchByNameAsync(string name); // Додати цей рядок
}