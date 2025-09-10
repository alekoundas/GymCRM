export interface PhoneNumberDto {
  id?: number; // Optional for new phone numbers
  number: string;
  isPrimary: boolean;
  userId?: string;
}
export class PhoneNumberDto {
  id?: number = 0; // Optional for new phone numbers
  number: string;
  isPrimary: boolean;
  userId?: string = "";
}
