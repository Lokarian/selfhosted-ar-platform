import {ArMemberDto, ArSessionDto} from "../../web-api-client";

export interface IRpcArService {
  UpdateArSession(arSession: ArSessionDto);

  UpdateArMember(arSessionMember: ArMemberDto);
}
