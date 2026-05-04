using Microsoft.AspNetCore.Mvc;
using DocumentManager.Core.Interfaces;
using DocumentManager.API.DTOs;
using DocumentManager.Core.Entities;
using DocumentManager.Core.Exceptions;

namespace DocumentManager.API.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;

    public DocumentsController(IDocumentService service)
    {
        _service = service;
    }

    // POST /api/documents
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Create([FromBody] CreateDocumentRequest request)
    {
        try
        {
            var doc = await _service.CreateAsync(
                request.FolderId,
                request.Name,
                request.ContentType,
                request.SizeBytes
            );

            return Ok(MapToDto(doc));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET /api/documents/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> Get(Guid id)
    {
        var doc = await _service.GetByIdAsync(id);

        if (doc == null)
            return NotFound();

        return Ok(MapToDto(doc));
    }

    // PUT /api/documents/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, [FromBody] UpdateDocumentRequest request)
    {
        try
        {
            var doc = await _service.UpdateAsync(
                id,
                request.Name,
                request.SizeBytes
            );

            return Ok(MapToDto(doc));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE /api/documents/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET /api/documents/{id}/versions
    [HttpGet("{id}/versions")]
    public async Task<ActionResult<List<DocumentVersionDto>>> GetVersions(Guid id)
    {
        var versions = await _service.GetVersionsAsync(id);

        var dto = versions.Select(v => new DocumentVersionDto
        {
            VersionNumber = v.VersionNumber,
            ChangeSummary = v.ChangeSummary,
            CreatedBy = v.CreatedBy,
            CreatedAt = v.CreatedAt
        }).ToList();

        return Ok(dto);
    }

    // GET /api/documents/search?name=...
    [HttpGet("search")]
public async Task<ActionResult<List<DocumentDto>>> Search([FromQuery] string name)
{
    var result = await _service.SearchByNameAsync(name);
    return Ok(result.Select(MapToDto).ToList());
}

    private static DocumentDto MapToDto(Document doc)
    {
        return new DocumentDto
        {
            Id = doc.Id,
            Name = doc.Name,
            ContentType = doc.ContentType,
            SizeBytes = doc.SizeBytes,
            Version = doc.Version,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt
        };
    }
}