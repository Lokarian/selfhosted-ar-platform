using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries.GetMyChatSessions;

public class ChatSessionDto : IMapFrom<ChatSession>
{
    public Guid BaseSessionId { get; set; }
    public SessionDto? BaseSession { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public IList<ChatMemberDto> Members { get; set; } = new List<ChatMemberDto>();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(d => d.LastMessage,
                opt => opt.MapFrom(s => s.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()))
            .ForMember(x => x.BaseSession, opt => opt.Ignore());
    }
}