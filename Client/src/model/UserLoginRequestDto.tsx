export interface UserLoginRequestDto {
  email: string;
  password: string;
}

export class UserLoginRequestDto {
  [key: string]: any;
  email: string = "";
  password: string = "";
}
