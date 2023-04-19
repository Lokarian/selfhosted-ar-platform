using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries;

public class ChatMessageDto : IMapFrom<ChatMessage>
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public Guid SenderId { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(d => d.SenderId, opt => opt.MapFrom(s => s.Sender.Id));
    }
}