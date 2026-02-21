namespace ContosoDashboard.Models;

public class DocumentScanJob
{
    public int DocumentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int UploadedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;
}
