using CoreServer.Application.AR.Queries.GetArSessionMembers;
using CoreServer.Application.AR.Queries.GetMyArSessions;
using CoreServer.Application.RPC.common;
namespace CoreServer.Application.RPC;

public interface IRpcArService : IRpcService
{
    Task UpdateArSession(ArSessionDto ArSession);
    Task UpdateArMember(ArMemberDto ArSessionMember);

}