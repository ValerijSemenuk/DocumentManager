namespace DocumentManager.Core.Entities;

public class Document
{
    public Guid Id { get; set; }

    public Guid FolderId { get; set; }

    public string Name { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public Folder Folder { get; set; } = null!;
    public List<DocumentVersion> Versions { get; set; } = new();
}