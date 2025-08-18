export interface UserLoginRequestDto {
  userName: string;
  email: string;
  password: string;
}

export class UserLoginRequestDto {
  [key: string]: any;
  userName: string = "";
  email: string = "";
  password: string = "";
}
