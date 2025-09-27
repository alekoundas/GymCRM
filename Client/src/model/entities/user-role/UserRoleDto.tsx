import { RoleDto } from "../role/RoleDto";
import { UserDto } from "../user/UserDto";

export interface UserRoleDto {
  [key: string]: any;
  user: UserDto;
  role: RoleDto;
}

export class UserRoleDto {
  user: UserDto = new UserDto();
  role: RoleDto = new RoleDto();
}
