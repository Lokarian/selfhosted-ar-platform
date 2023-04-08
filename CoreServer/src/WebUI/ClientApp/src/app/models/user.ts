export interface User {
  id: number;
  name: string;
  image: string;
  status: OnlineStatus;
}
export enum OnlineStatus {
  Online = 'online',
  Offline = 'offline',
  Away = 'away',
  Busy = 'busy'
}
