using System.Text.Json;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedCategories =
    [
        "Project Documents",
        "Team Resources",
        "Personal Files",
        "Reports",
        "Presentations",
        "Other"
    ];

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"
    ];

    private static readonly HashSet<string> PreviewMimeTypes =
    [
        "application/pdf", "image/jpeg", "image/png"
    ];

    private const long MaxSize = 25 * 1024 * 1024;

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IScanQueueService _scanQueueService;
    private readonly INotificationService _notificationService;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IScanQueueService scanQueueService,
        INotificationService notificationService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _scanQueueService = scanQueueService;
        _notificationService = notificationService;
    }

    public async Task<Document> UploadDocumentAsync(int requestingUserId, DocumentUploadRequest request, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        ValidateUploadRequest(request, fileName, fileSize);
        await EnsureCanUploadToProjectAsync(request.ProjectId, requestingUserId, cancellationToken);

        var filePath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType, requestingUserId, request.ProjectId, cancellationToken);

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            FileName = fileName,
            FilePath = filePath,
            FileType = contentType,
            FileSizeBytes = fileSize,
            UploadedByUserId = requestingUserId,
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            UploadedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ScanStatus = DocumentScanStatus.Pending,
            ScanRequestedAtUtc = DateTime.UtcNow
        };

        _context.Set<Document>().Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _context.Set<DocumentTag>().Add(new DocumentTag
            {
                DocumentId = document.DocumentId,
                TagValue = tag,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _context.Set<DocumentActivity>().AddAsync(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            ActivityType = DocumentActivityType.Upload,
            MetadataJson = JsonSerializer.Serialize(new { document.Category, document.ProjectId })
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _scanQueueService.EnqueueScanAsync(new DocumentScanJob
        {
            DocumentId = document.DocumentId,
            FilePath = document.FilePath,
            FileType = document.FileType,
            UploadedByUserId = requestingUserId,
            ProjectId = document.ProjectId,
            EnqueuedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        if (document.ProjectId.HasValue)
        {
            var memberIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == document.ProjectId.Value && pm.UserId != requestingUserId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var memberId in memberIds)
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = memberId,
                    Title = "New Project Document",
                    Message = $"A new document '{document.Title}' was added to your project.",
                    Type = NotificationType.ProjectUpdate,
                    Priority = NotificationPriority.Informational
                });
            }
        }

        return document;
    }

    public async Task<List<Document>> GetDocumentsAsync(int requestingUserId, DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var scoped = BuildAccessScopedQuery(requestingUserId);

        if (!string.IsNullOrWhiteSpace(query.Category))
            scoped = scoped.Where(d => d.Category == query.Category);

        if (query.ProjectId.HasValue)
            scoped = scoped.Where(d => d.ProjectId == query.ProjectId.Value);

        if (query.FromDate.HasValue)
            scoped = scoped.Where(d => d.UploadedAtUtc.Date >= query.FromDate.Value.Date);

        if (query.ToDate.HasValue)
            scoped = scoped.Where(d => d.UploadedAtUtc.Date <= query.ToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var q = query.Search.Trim().ToLower();
            scoped = scoped.Where(d =>
                d.Title.ToLower().Contains(q) ||
                (d.Description != null && d.Description.ToLower().Contains(q)) ||
                d.UploadedByUser.DisplayName.ToLower().Contains(q) ||
                (d.Project != null && d.Project.Name.ToLower().Contains(q)) ||
                d.Tags.Any(t => t.TagValue.ToLower().Contains(q)));
        }

        scoped = ApplySort(scoped, query.SortBy, query.SortDir);

        return await scoped
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Include(d => d.Tags)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await HasProjectAccessAsync(projectId, requestingUserId, cancellationToken);
        if (!hasAccess)
        {
            return new List<Document>();
        }

        return await _context.Set<Document>()
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted && d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Document>()
            .Include(d => d.UploadedByUser)
            .Where(d => !d.IsDeleted && d.Shares.Any(s => s.SharedWithUserId == requestingUserId))
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Set<Document>()
            .Include(d => d.UploadedByUser)
            .Include(d => d.Tags)
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);

        if (document == null)
            return null;

        var hasAccess = await HasDocumentAccessAsync(document, requestingUserId, cancellationToken);
        return hasAccess ? document : null;
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        EnsureScanned(document);

        var stream = await _fileStorageService.DownloadAsync(document.FilePath, cancellationToken);

        await LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Download, cancellationToken: cancellationToken);

        return (stream, document.FileType, document.FileName);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> PreviewDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        EnsureScanned(document);

        if (!PreviewMimeTypes.Contains(document.FileType.ToLowerInvariant()))
            throw new InvalidOperationException("Preview not supported for this file type.");

        var stream = await _fileStorageService.DownloadAsync(document.FilePath, cancellationToken);
        await LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Preview, cancellationToken: cancellationToken);

        return (stream, document.FileType, document.FileName);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, DocumentUpdateMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        if (!await CanManageDocumentAsync(document, requestingUserId, cancellationToken))
            return false;

        if (!string.IsNullOrWhiteSpace(request.Title))
            document.Title = request.Title.Trim();

        document.Description = request.Description?.Trim();

        if (!string.IsNullOrWhiteSpace(request.Category) && AllowedCategories.Contains(request.Category))
            document.Category = request.Category;

        document.UpdatedAtUtc = DateTime.UtcNow;

        var existingTags = await _context.Set<DocumentTag>().Where(t => t.DocumentId == documentId).ToListAsync(cancellationToken);
        _context.Set<DocumentTag>().RemoveRange(existingTags);

        foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _context.Set<DocumentTag>().AddAsync(new DocumentTag
            {
                DocumentId = documentId,
                TagValue = tag,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await LogActivityAsync(documentId, requestingUserId, DocumentActivityType.MetadataEdit, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        if (!await CanManageDocumentAsync(document, requestingUserId, cancellationToken))
            return false;

        ValidateUploadRequest(new DocumentUploadRequest { Title = document.Title, Category = document.Category }, fileName, fileSize);

        await _fileStorageService.DeleteAsync(document.FilePath, cancellationToken);
        var newFilePath = await _fileStorageService.UploadAsync(fileStream, fileName, contentType, requestingUserId, document.ProjectId, cancellationToken);

        document.FileName = fileName;
        document.FilePath = newFilePath;
        document.FileType = contentType;
        document.FileSizeBytes = fileSize;
        document.ScanStatus = DocumentScanStatus.Pending;
        document.ScanRequestedAtUtc = DateTime.UtcNow;
        document.ScanCompletedAtUtc = null;
        document.ScanFailureReason = null;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await LogActivityAsync(documentId, requestingUserId, DocumentActivityType.Replace, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _scanQueueService.EnqueueScanAsync(new DocumentScanJob
        {
            DocumentId = document.DocumentId,
            FilePath = document.FilePath,
            FileType = document.FileType,
            UploadedByUserId = document.UploadedByUserId,
            ProjectId = document.ProjectId,
            EnqueuedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        if (!await CanManageDocumentAsync(document, requestingUserId, cancellationToken))
            return false;

        await _fileStorageService.DeleteAsync(document.FilePath, cancellationToken);

        document.IsDeleted = true;
        document.DeletedByUserId = requestingUserId;
        document.DeletedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await LogActivityAsync(documentId, requestingUserId, DocumentActivityType.Delete, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int requestingUserId, DocumentShareRequest request, CancellationToken cancellationToken = default)
    {
        var document = await EnsureAccessibleDocument(documentId, requestingUserId, cancellationToken);
        if (!await CanManageDocumentAsync(document, requestingUserId, cancellationToken))
            return false;

        var userIds = request.UserIds.Distinct().Where(id => id != requestingUserId).ToList();
        foreach (var userId in userIds)
        {
            var exists = await _context.Set<DocumentShare>()
                .AnyAsync(s => s.DocumentId == documentId && s.SharedWithUserId == userId, cancellationToken);

            if (!exists)
            {
                await _context.Set<DocumentShare>().AddAsync(new DocumentShare
                {
                    DocumentId = documentId,
                    SharedByUserId = requestingUserId,
                    SharedWithUserId = userId,
                    SharedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }

            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "Document Shared",
                Message = $"{document.Title} was shared with you.",
                Type = NotificationType.ProjectUpdate,
                Priority = NotificationPriority.Important
            });

            await LogActivityAsync(documentId, requestingUserId, DocumentActivityType.Share, userId, cancellationToken: cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Document>> GetRecentUserDocumentsAsync(int userId, int top = 5, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Document>()
            .Where(d => !d.IsDeleted && d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Take(top)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUserDocumentCountAsync(int userId, CancellationToken cancellationToken = default)
        => _context.Set<Document>().CountAsync(d => !d.IsDeleted && d.UploadedByUserId == userId, cancellationToken);

    private IQueryable<Document> BuildAccessScopedQuery(int requestingUserId)
    {
        return _context.Set<Document>()
            .Where(d => !d.IsDeleted)
            .Where(d =>
                d.UploadedByUserId == requestingUserId ||
                d.Shares.Any(s => s.SharedWithUserId == requestingUserId) ||
                (d.ProjectId != null && d.Project!.ProjectMembers.Any(pm => pm.UserId == requestingUserId)) ||
                (d.ProjectId != null && d.Project!.ProjectManagerId == requestingUserId) ||
                _context.Users.Any(u => u.UserId == requestingUserId && u.Role == UserRole.Administrator));
    }

    private IQueryable<Document> ApplySort(IQueryable<Document> query, string? sortBy, string? sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToLowerInvariant()) switch
        {
            "title" => desc ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
            "category" => desc ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
            "filesizebytes" => desc ? query.OrderByDescending(d => d.FileSizeBytes) : query.OrderBy(d => d.FileSizeBytes),
            _ => desc ? query.OrderByDescending(d => d.UploadedAtUtc) : query.OrderBy(d => d.UploadedAtUtc)
        };
    }

    private async Task<Document> EnsureAccessibleDocument(int documentId, int requestingUserId, CancellationToken cancellationToken)
    {
        var document = await _context.Set<Document>()
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Document not found.");

        if (!await HasDocumentAccessAsync(document, requestingUserId, cancellationToken))
            throw new UnauthorizedAccessException("Not authorized to access this document.");

        return document;
    }

    private async Task<bool> HasDocumentAccessAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploadedByUserId == requestingUserId)
            return true;

        if (document.Shares.Any(s => s.SharedWithUserId == requestingUserId))
            return true;

        if (document.ProjectId.HasValue && await HasProjectAccessAsync(document.ProjectId.Value, requestingUserId, cancellationToken))
            return true;

        var user = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        return user?.Role == UserRole.Administrator;
    }

    private async Task<bool> CanManageDocumentAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploadedByUserId == requestingUserId)
            return true;

        if (document.ProjectId.HasValue)
        {
            var project = await _context.Projects.FindAsync([document.ProjectId.Value], cancellationToken);
            if (project != null && project.ProjectManagerId == requestingUserId)
                return true;
        }

        var user = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        return user?.Role == UserRole.Administrator;
    }

    private async Task<bool> HasProjectAccessAsync(int projectId, int requestingUserId, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);

        if (project == null)
            return false;

        if (project.ProjectManagerId == requestingUserId)
            return true;

        if (project.ProjectMembers.Any(pm => pm.UserId == requestingUserId))
            return true;

        var user = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        return user?.Role == UserRole.Administrator;
    }

    private async Task EnsureCanUploadToProjectAsync(int? projectId, int requestingUserId, CancellationToken cancellationToken)
    {
        if (!projectId.HasValue)
            return;

        if (!await HasProjectAccessAsync(projectId.Value, requestingUserId, cancellationToken))
            throw new UnauthorizedAccessException("Not authorized for selected project.");
    }

    private static void ValidateUploadRequest(DocumentUploadRequest request, string fileName, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Category) || !AllowedCategories.Contains(request.Category))
            throw new InvalidOperationException("Valid category is required.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file type.");

        if (fileSize <= 0 || fileSize > MaxSize)
            throw new InvalidOperationException("File exceeds maximum size of 25 MB.");
    }

    private static void EnsureScanned(Document document)
    {
        if (document.ScanStatus == DocumentScanStatus.Pending)
            throw new InvalidOperationException("Document scan is pending.");

        if (document.ScanStatus == DocumentScanStatus.Rejected)
            throw new InvalidOperationException("Document failed malware scan.");
    }

    private async Task LogActivityAsync(int documentId, int actorUserId, DocumentActivityType type, int? targetUserId = null, string? metadata = null, CancellationToken cancellationToken = default)
    {
        await _context.Set<DocumentActivity>().AddAsync(new DocumentActivity
        {
            DocumentId = documentId,
            ActorUserId = actorUserId,
            ActivityType = type,
            TargetUserId = targetUserId,
            MetadataJson = metadata,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
