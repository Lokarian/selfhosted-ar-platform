using System.ComponentModel.DataAnnotations.Schema;

namespace CoreServer.Domain.Common;

public abstract class BaseEntity : EntityWithEvents
{
    public Guid Id { get; set; } = Guid.NewGuid();
}