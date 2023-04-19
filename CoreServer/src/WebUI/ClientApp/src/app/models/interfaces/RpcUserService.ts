import {AppUserDto} from "../../web-api-client";


export interface IRpcUserService
{
  UpdateUser(user: AppUserDto);
}
