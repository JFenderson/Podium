namespace Podium.Core.Entities;

public class Document : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeInBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    // Metadata
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Status
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;
    public int Version { get; set; } = 1;

    // Access Control
    public bool IsPublic { get; set; } = false;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
}

public enum DocumentStatus
{
    Active = 1,
    Archived = 2,
    Deleted = 3
}