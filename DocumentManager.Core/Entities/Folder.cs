namespace DocumentManager.Core.Entities;

public class Folder
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid? ParentFolderId { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Folder? ParentFolder { get; set; }
    public List<Folder> SubFolders { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
}