using Microsoft.AspNetCore.Mvc;
using DocumentManager.Core.Interfaces;
using DocumentManager.Core.Entities;
using DocumentManager.API.DTOs;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/folders")]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _service;

    public FoldersController(IFolderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<FolderDto>>> GetRoot()
    {
        var folders = await _service.GetRootFoldersAsync();
        return Ok(folders.Select(MapFolder).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FolderDto>> Get(Guid id)
    {
        var folder = await _service.GetByIdAsync(id);
        if (folder == null)
            return NotFound();

        return Ok(MapFolder(folder));
    }

    [HttpPost]
    public async Task<ActionResult<FolderDto>> Create(CreateFolderRequest request)
    {
        var folder = await _service.CreateAsync(
            request.Name,
            request.ParentFolderId,
            request.CreatedBy ?? "test"
        );
        return Ok(MapFolder(folder));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    private FolderDto MapFolder(Folder folder)
    {
        return new FolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            SubFolders = folder.SubFolders?.Select(MapFolder).ToList(),
            Documents = folder.Documents?.Select(d => new DocumentDto
            {
                Id = d.Id,
                Name = d.Name,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                Version = d.Version,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList()
        };
    }
}

public class CreateFolderRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentFolderId { get; set; }
    public string? CreatedBy { get; set; }
}