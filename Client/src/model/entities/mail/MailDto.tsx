import { BaseDto } from "../BaseDto";
import { UserDto } from "../user/UserDto";

export interface MailDto extends BaseDto {
  id?: number; // Optional for add mode
  subject: string;
  body: string;
  userId?: string;
  user?: UserDto;
}
export class MailDto extends BaseDto {
  id?: number = 0; // Optional for add mode
  subject: string = "";
  body: string = "";
  userId?: string = "";
  user?: UserDto = new UserDto();
}
