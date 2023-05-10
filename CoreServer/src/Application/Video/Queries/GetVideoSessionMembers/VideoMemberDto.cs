using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Video.Queries.Dtos;
using CoreServer.Domain.Entities.Video;

namespace CoreServer.Application.Video.Queries;

public class VideoMemberDto : IMapFrom<VideoMember>
{
    public Guid Id { get; set; }
    public Guid BaseMemberId { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<VideoMember, VideoMemberDto>()
            .ForMember(d => d.UserId,
                opt => opt.MapFrom(s => s.BaseMember.UserId));
    }
}