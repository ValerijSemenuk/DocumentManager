namespace DocumentManager.API.DTOs;

public class UpdateDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}