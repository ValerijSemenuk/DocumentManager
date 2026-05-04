namespace DocumentManager.API.DTOs;

public class DocumentVersionDto
{
    public int VersionNumber { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}