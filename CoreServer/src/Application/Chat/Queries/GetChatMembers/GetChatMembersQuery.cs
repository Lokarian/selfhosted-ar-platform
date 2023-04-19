using MediatR;

namespace CoreServer.Application.Chat.Queries.GetSessionMembers;

public class GetChatMembersQuery : IRequest<IList<ChatMemberDto>>
{
    public Guid SessionId { get; set; }
}