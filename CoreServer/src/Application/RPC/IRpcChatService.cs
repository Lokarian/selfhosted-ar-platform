using CoreServer.Application.Chat.Queries;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.RPC.common;

namespace CoreServer.Application.RPC;

public interface IRpcChatService : IRpcService
{
    Task UpdateChatSession(ChatSessionDto chatSession);
    Task NewChatMessage(ChatMessageDto chatMessage);
    Task UpdateChatMember(ChatMemberDto chatMember);
}