using CoreServer.Application.TodoLists.Queries.ExportTodos;

namespace CoreServer.Application.Common.Interfaces;

public interface ICsvFileBuilder
{
    byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records);
}
