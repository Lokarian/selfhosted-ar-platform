using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Application.AR.Queries.GetArSessionMembers;

public class ArMemberDto : IMapFrom<ArMember>
{
    public Guid Id { get; set; }
    public Guid BaseMemberId { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public ArUserRole Role { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArMember, ArMemberDto>()
            .ForMember(d => d.UserId,
                opt => opt.MapFrom(s => s.BaseMember.UserId));
    }
}