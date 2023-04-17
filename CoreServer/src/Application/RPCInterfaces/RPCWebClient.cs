using CoreServer.Application.Chat.Queries;
using CoreServer.Application.User.Queries;

namespace CoreServer.Application.RPCInterfaces;

public interface RPCWebClient
{
    Task UpdateUser(AppUserDto user);
    Task UpdateChatSession(ChatSessionDto chatSession);
    Task NewChatMessage(ChatMessageDto chatMessage);
}