using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMembers;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries.GetMyChatSessions;

public class ChatSessionDto : IMapFrom<ChatSession>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public ChatMessageDto? LastMessage { get; set; }
    public IList<ChatMemberDto> Members { get; set; } = new List<ChatMemberDto>();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(d => d.LastMessage,
                opt => opt.MapFrom(s => s.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()));
    }
}