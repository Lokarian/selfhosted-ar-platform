import {User} from "./user";

export interface ChatMessage {
  message: string;
  sender: User;
  timestamp: number;
}
