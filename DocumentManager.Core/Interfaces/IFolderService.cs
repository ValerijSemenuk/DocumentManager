using DocumentManager.Core.Entities;

namespace DocumentManager.Core.Interfaces;

public interface IFolderService
{
    Task<List<Folder>> GetRootFoldersAsync();
    Task<Folder?> GetByIdAsync(Guid id);

    Task<Folder> CreateAsync(string name, Guid? parentId, string createdBy);

    Task DeleteAsync(Guid id);
}