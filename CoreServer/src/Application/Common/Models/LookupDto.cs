using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.Common.Models;

// Note: This is currently just used to demonstrate applying multiple IMapFrom attributes.
public class LookupDto : IMapFrom<TodoList>, IMapFrom<TodoItem>
{
    public Guid Id { get; set; }

    public string? Title { get; set; }
}