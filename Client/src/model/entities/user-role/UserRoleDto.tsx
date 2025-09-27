import { RoleDto } from "../role/RoleDto";
import { UserDto } from "../user/UserDto";

export interface UserRoleDto {
  [key: string]: any;
  user?: UserDto | undefined;
  role?: RoleDto | undefined;
}

export class UserRoleDto {
  user?: UserDto | undefined;
  role?: RoleDto | undefined;
}
