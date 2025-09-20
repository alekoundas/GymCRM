import { BaseDto } from "../BaseDto";

export interface MailDto extends BaseDto {
  id?: number; // Optional for add mode
  subject: string;
  body: string;
  userId?: string;
}
export class MailDto extends BaseDto {
  id?: number = 0; // Optional for add mode
  subject: string = "";
  body: string = "";
  userId?: string = "";
}
