import {AppUserDto, ChatMessageDto, ChatSessionDto} from "../../web-api-client";

export interface IRPCWebClient {
  updateUser(user: AppUserDto);
  updateChatSession(chatSession:ChatSessionDto);
  newChatMessage(chatMessage:ChatMessageDto);
}
