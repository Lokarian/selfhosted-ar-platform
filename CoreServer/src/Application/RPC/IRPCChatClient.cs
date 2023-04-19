using CoreServer.Application.Chat.Queries;

namespace CoreServer.Application.RPCInterfaces;

public interface IRpcChatClient:IRpcService
{
    Task UpdateChatSession(ChatSessionDto chatSession);
    Task NewChatMessage(ChatMessageDto chatMessage);
}