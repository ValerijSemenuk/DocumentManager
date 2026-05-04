namespace DocumentManager.API.DTOs;

public class CreateDocumentRequest
{
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}