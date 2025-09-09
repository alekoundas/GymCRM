import { PhoneNumber } from "../phone-number/PhoneNumber";

export interface UserRegisterDto {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  phoneNumbers: PhoneNumber[];
}

export class UserRegisterDto {
  [key: string]: any;
  userName: string = "";
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  password: string = "";
  phoneNumbers: PhoneNumber[];
}
