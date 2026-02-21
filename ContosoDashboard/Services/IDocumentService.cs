using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentQuery
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public string? Search { get; set; }
}

public class DocumentShareRequest
{
    public List<int> UserIds { get; set; } = new();
    public List<string> TeamKeys { get; set; } = new();
}

public class DocumentUpdateMetadataRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
}

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(int requestingUserId, DocumentUploadRequest request, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsAsync(int requestingUserId, DocumentQuery query, CancellationToken cancellationToken = default);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)> PreviewDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, DocumentUpdateMetadataRequest request, CancellationToken cancellationToken = default);
    Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ShareDocumentAsync(int documentId, int requestingUserId, DocumentShareRequest request, CancellationToken cancellationToken = default);
    Task<List<Document>> GetRecentUserDocumentsAsync(int userId, int top = 5, CancellationToken cancellationToken = default);
    Task<int> GetUserDocumentCountAsync(int userId, CancellationToken cancellationToken = default);
}
