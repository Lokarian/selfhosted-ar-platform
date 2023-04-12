import { AppUser } from "../web-api-client";

export interface ChatMessage {
  message: string;
  sender: AppUser;
  timestamp: number;
}
