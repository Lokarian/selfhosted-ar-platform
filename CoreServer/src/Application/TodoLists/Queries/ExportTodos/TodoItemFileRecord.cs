using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.TodoLists.Queries.ExportTodos;

public class TodoItemRecord : IMapFrom<TodoItem>
{
    public string? Title { get; set; }

    public bool Done { get; set; }
}
