export interface UserRegisterRequestDto {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

export class UserRegisterRequestDto {
  [key: string]: any;
  userName: string = "";
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  password: string = "";
}
