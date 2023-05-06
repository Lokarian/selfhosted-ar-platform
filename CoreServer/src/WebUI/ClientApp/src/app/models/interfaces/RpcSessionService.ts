import {SessionDto, SessionMemberDto} from "../../web-api-client";

export interface IRpcSessionService {
  UpdateSession(session: SessionDto);

  UpdateSessionMember(sessionMember: SessionMemberDto);

  RemoveSession(id: string);
}
