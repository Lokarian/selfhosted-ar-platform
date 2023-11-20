using AutoMapper;
using CoreServer.Application.AR.Queries.GetArSessionMembers;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Domain.Entities.AR;

namespace CoreServer.Application.AR.Queries.GetMyArSessions;

public class ArSessionDto : IMapFrom<ArSession>
{
    public Guid BaseSessionId { get; set; }
    public SessionDto? BaseSession { get; set; }
    public IList<ArMemberDto> Members { get; set; } = new List<ArMemberDto>();
    public ArSessionType SessionType { get; set; }
    public ArServerState ServerState { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArSession, ArSessionDto>()
            .ForMember(x => x.BaseSession, opt => opt.Ignore());
    }
}