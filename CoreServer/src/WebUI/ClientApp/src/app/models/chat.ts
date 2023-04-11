import {AppUser} from "./appUser";

export interface ChatMessage {
  message: string;
  sender: AppUser;
  timestamp: number;
}
