using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Video.Queries.GetMyVideoSessions;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Application.Session.Queries.GetMySessions;

public class SessionDto : IMapFrom<UserSession>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public IList<SessionMemberDto> Members { get; set; } = new List<SessionMemberDto>();
    public ChatSessionDto? ChatSession { get; set; }
    public VideoSessionDto? VideoSession { get; set; }
    
}