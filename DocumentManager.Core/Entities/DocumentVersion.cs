using DocumentManager.Core.Entities;

public class DocumentVersion
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int VersionNumber { get; set; }

    public string ChangeSummary { get; set; }

    public string CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Document Document { get; set; }
}