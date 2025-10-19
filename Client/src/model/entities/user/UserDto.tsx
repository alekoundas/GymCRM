import { UserRoleDto } from "../user-role/UserRoleDto";
import { UserStatusDto } from "../user-status/UserStatusDto";

export interface UserDto {
  [key: string]: any;

  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
  userStatusId?: number;
  userStatus?: UserStatusDto;
  userRoles: UserRoleDto[];
}

export class UserDto {
  id: string = "";
  userName: string = "";
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
  userStatusId?: number;
  userStatus?: UserStatusDto;
  userRoles: UserRoleDto[] = [];
}
