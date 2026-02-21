using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentUploadRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public List<string> Tags { get; set; } = new();
}
