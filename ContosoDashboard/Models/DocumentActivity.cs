using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    public DocumentActivityType ActivityType { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public int? TargetUserId { get; set; }

    [MaxLength(4000)]
    public string? MetadataJson { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;

    [ForeignKey(nameof(TargetUserId))]
    public virtual User? TargetUser { get; set; }
}
