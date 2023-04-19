using MediatR;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class GetChatMembersQuery : IRequest<IList<ChatMemberDto>>
{
    public Guid SessionId { get; set; }
}