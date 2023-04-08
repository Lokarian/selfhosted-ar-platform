using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.TodoLists.Commands.CreateTodoList;
using CoreServer.Application.TodoLists.Commands.DeleteTodoList;
using CoreServer.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace CoreServer.Application.IntegrationTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(Guid.Parse("f056c31a-4895-4e5e-b6ed-0eb80ad4a4d1"));
        await FluentActions.Invoking(() => SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindAsync<TodoList>(listId);

        list.Should().BeNull();
    }
}
