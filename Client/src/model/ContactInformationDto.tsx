import { ContactInformationTypesEnum } from "../enum/ContactInformationTypesEnum";
import { CustomerDto } from "./CustomerDto";

export class ContactInformationDto {
  id: number = 0;
  type: ContactInformationTypesEnum = ContactInformationTypesEnum.PHONE;
  value: string = "";
  description: string = "";
  isFavorite: boolean = false;

  customerId: number | null = null;
  customer: CustomerDto | null = null;
}

export interface ContactInformationDto {
  [key: string]: any;
  id: number;
  type: ContactInformationTypesEnum;
  value: string;
  description: string;
  isFavorite: boolean;

  customerId: number | null;
  customer: CustomerDto | null;
}
