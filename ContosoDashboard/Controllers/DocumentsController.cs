using System.Security.Claims;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Document>>> GetDocuments(
        [FromQuery] string? category,
        [FromQuery] int? projectId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var results = await _documentService.GetDocumentsAsync(userId.Value, new DocumentQuery
        {
            Category = category,
            ProjectId = projectId,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = sortBy,
            SortDir = sortDir,
            Search = search
        }, cancellationToken);

        return Ok(results);
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<List<Document>>> GetProjectDocuments(int projectId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var results = await _documentService.GetProjectDocumentsAsync(projectId, userId.Value, cancellationToken);
        return Ok(results);
    }

    [HttpGet("shared")]
    public async Task<ActionResult<List<Document>>> GetSharedWithMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var results = await _documentService.GetSharedWithMeAsync(userId.Value, cancellationToken);
        return Ok(results);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(26214400)]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadApiRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { error = "File is required." });

        await using var stream = request.File.OpenReadStream();

        var uploaded = await _documentService.UploadDocumentAsync(
            userId.Value,
            new DocumentUploadRequest
            {
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                Tags = request.Tags
            },
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        return Accepted(new
        {
            documentId = uploaded.DocumentId,
            uploaded.Title,
            uploaded.ScanStatus,
            uploaded.UploadedAtUtc
        });
    }

    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _documentService.DownloadDocumentAsync(documentId, userId.Value, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpGet("{documentId:int}/preview")]
    public async Task<IActionResult> Preview(int documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _documentService.PreviewDocumentAsync(documentId, userId.Value, cancellationToken);
        return File(result.Stream, result.ContentType);
    }

    [HttpPut("{documentId:int}/metadata")]
    public async Task<IActionResult> UpdateMetadata(int documentId, [FromBody] DocumentUpdateMetadataRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var updated = await _documentService.UpdateMetadataAsync(documentId, userId.Value, request, cancellationToken);
        return updated ? NoContent() : Forbid();
    }

    [HttpPut("{documentId:int}/replace")]
    [RequestSizeLimit(26214400)]
    public async Task<IActionResult> ReplaceFile(int documentId, [FromForm] ReplaceDocumentApiRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { error = "File is required." });

        await using var stream = request.File.OpenReadStream();

        var replaced = await _documentService.ReplaceFileAsync(
            documentId,
            userId.Value,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        return replaced ? NoContent() : Forbid();
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var deleted = await _documentService.DeleteDocumentAsync(documentId, userId.Value, cancellationToken);
        return deleted ? NoContent() : Forbid();
    }

    [HttpPost("{documentId:int}/share")]
    public async Task<IActionResult> Share(int documentId, [FromBody] DocumentShareRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var shared = await _documentService.ShareDocumentAsync(documentId, userId.Value, request, cancellationToken);
        return shared ? NoContent() : Forbid();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

public class DocumentUploadApiRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public List<string> Tags { get; set; } = new();
    public IFormFile? File { get; set; }
}

public class ReplaceDocumentApiRequest
{
    public IFormFile? File { get; set; }
}
