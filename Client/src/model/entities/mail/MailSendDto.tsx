import { BaseDto } from "../BaseDto";
import { UserDto } from "../user/UserDto";

export interface MailSendDto extends BaseDto {
  subject: string;
  body: string;
  userIds?: string[];
}
export class MailSendDto extends BaseDto {
  subject: string = "";
  body: string = "";
  userIds?: string[] = [];
}
