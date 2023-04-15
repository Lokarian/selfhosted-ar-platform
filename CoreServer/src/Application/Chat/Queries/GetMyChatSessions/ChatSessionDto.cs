using AutoMapper;
using CoreServer.Application.Chat.Queries.GetSessionMembers;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries;

public class ChatSessionDto : IMapFrom<ChatSession>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public ChatMessage LastMessage { get; set; } = null!;
    public IList<ChatMemberDto> Members { get; set; } = new List<ChatMemberDto>();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(d => d.LastMessage,
                opt => opt.MapFrom(s => s.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()));
    }
}