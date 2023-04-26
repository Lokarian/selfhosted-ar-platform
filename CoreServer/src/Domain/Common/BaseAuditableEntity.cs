namespace CoreServer.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;

    public Guid? CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; } = null!;

    public DateTime? LastModified { get; set; }

    public Guid? LastModifiedById { get; set; }

    public AppUser? LastModifiedBy { get; set; } = null!;
}