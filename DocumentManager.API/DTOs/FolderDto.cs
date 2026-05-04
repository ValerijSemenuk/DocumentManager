namespace DocumentManager.API.DTOs;

public class FolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FolderDto> SubFolders { get; set; } = new();
    public List<DocumentDto> Documents { get; set; } = new();
}