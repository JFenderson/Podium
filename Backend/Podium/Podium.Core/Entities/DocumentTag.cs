namespace DocumentManagement.Core.Entities;

public class DocumentTag : BaseEntity
{
    public int DocumentId { get; set; }
    public string TagName { get; set; } = string.Empty;

    // Navigation properties
    public virtual Document? Document { get; set; } = null!;
}