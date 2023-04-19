using CoreServer.Application.Chat.Queries;
using CoreServer.Application.RPC.common;

namespace CoreServer.Application.RPC;

public interface IRpcChatService : IRpcService
{
    Task UpdateChatSession(ChatSessionDto chatSession);
    Task NewChatMessage(ChatMessageDto chatMessage);
}