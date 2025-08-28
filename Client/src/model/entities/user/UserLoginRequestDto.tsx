export interface UserLoginRequestDto {
  userNameOrEmail: string;
  password: string;
}

export class UserLoginRequestDto {
  [key: string]: any;
  userNameOrEmail: string = "";
  password: string = "";
}
