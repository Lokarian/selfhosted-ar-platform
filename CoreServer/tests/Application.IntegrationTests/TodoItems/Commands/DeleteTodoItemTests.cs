using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.TodoItems.Commands.CreateTodoItem;
using CoreServer.Application.TodoItems.Commands.DeleteTodoItem;
using CoreServer.Application.TodoLists.Commands.CreateTodoList;
using CoreServer.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace CoreServer.Application.IntegrationTests.TodoItems.Commands;

using static Testing;

public class DeleteTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoItemId()
    {
        var command = new DeleteTodoItemCommand(Guid.Parse("f056c31a-4895-4e5e-b6ed-0eb80ad4a4d1"));

        await FluentActions.Invoking(() =>
            SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTodoItem()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var itemId = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        });

        await SendAsync(new DeleteTodoItemCommand(itemId));

        var item = await FindAsync<TodoItem>(itemId);

        item.Should().BeNull();
    }
}
