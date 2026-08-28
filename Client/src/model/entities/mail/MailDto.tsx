import { BaseDto } from "../BaseDto";
import { UserDto } from "../user/UserDto";
import { MailStatusEnum } from "../../../enum/MailStatusEnum";

export interface MailDto extends BaseDto {
  id?: number; // Optional for add mode
  subject: string;
  body: string;
  status?: MailStatusEnum;
  sentOn?: string;
  error?: string;
  userId?: string;
  user?: UserDto;
}
export class MailDto extends BaseDto {
  id?: number = 0; // Optional for add mode
  subject: string = "";
  body: string = "";
  status?: MailStatusEnum = MailStatusEnum.PENDING;
  sentOn?: string = undefined;
  error?: string = "";
  userId?: string = "";
  user?: UserDto = new UserDto();
}
