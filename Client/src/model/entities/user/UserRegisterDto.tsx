import { PhoneNumberDto } from "../phone-number/PhoneNumberDto";

export interface UserRegisterDto {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  address: string;
  password: string;
  phoneNumbers: PhoneNumberDto[];
}

export class UserRegisterDto {
  [key: string]: any;
  userName: string = "";
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  address: string = "";
  password: string = "";
  phoneNumbers: PhoneNumberDto[] = [];
}
