export interface UserDto {
  [key: string]: any;

  id: string;
  userName: string;
  email: string;
  roleId: string;
}

export class UserDto {
  id: string = "";
  userName: string = "";
  email: string = "";
  roleId: string = "";
}
