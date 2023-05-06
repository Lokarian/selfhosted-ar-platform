using CoreServer.Application.Chat.Queries;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Session.Queries.GetMySessions;

namespace CoreServer.Application.RPC;

public interface IRpcSessionService : IRpcService
{
    Task UpdateSession(SessionDto session);
    Task UpdateSessionMember(SessionMemberDto sessionMember);
    Task RemoveSession(Guid id);
}