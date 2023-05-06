using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;

namespace CoreServer.Application.Session.Queries.GetMySessions;

public class SessionMemberDto : IMapFrom<SessionMember>
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
}