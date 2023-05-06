import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";

export interface IRpcChatService
{
  UpdateChatSession(chatSession:ChatSessionDto);
  NewChatMessage(chatMessage:ChatMessageDto);
  UpdateChatMember(appUser:AppUserDto);
}
