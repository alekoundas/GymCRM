import { BaseDto } from "../BaseDto";
import { UserDto } from "../user/UserDto";

export interface MailSendDto extends BaseDto {
  id?: number; // Optional for add mode
  subject: string;
  body: string;
  userIds?: string[];
  users?: UserDto[];
}
export class MailSendDto extends BaseDto {
  id?: number = 0; // Optional for add mode
  subject: string = "";
  body: string = "";
  usersIds?: string[] = [];
  users?: UserDto[] = [];
}
