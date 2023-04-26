using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Chat;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class ChatMemberDto : IMapFrom<ChatMember>
{
    public Guid UserId { get; set; }
    public DateTime? LastSeen { get; set; }
    public Guid SessionId { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ChatMember, ChatMemberDto>()
            .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.User.Id));
    }
}