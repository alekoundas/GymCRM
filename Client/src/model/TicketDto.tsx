import { TicketStatusEnum } from "../enum/TicketStatusEnum";
import { TicketTypesEnum } from "../enum/TicketTypesEnum";
import { CustomerDto } from "./CustomerDto";

export class TicketDto {
  id: number = 0;
  status: TicketStatusEnum = TicketStatusEnum.OPENED;
  type: TicketTypesEnum = TicketTypesEnum.TV;
  description: string = "";
  descriptionHTML: string = "";
  completedOn: Date | null = null;
  customerId: number | null = null;
  customer: CustomerDto | null = null;
}

export interface TicketDto {
  [key: string]: any;
  id: number;
  status: TicketStatusEnum;
  type: TicketTypesEnum;
  description: string;
  descriptionHTML: string;
  completedOn: Date | null;
  customerId: number | null;
  customer: CustomerDto | null;
}
