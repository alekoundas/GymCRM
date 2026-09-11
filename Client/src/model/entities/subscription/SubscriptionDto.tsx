import { UserDto } from "../user/UserDto";
import { SubscriptionStatusEnum } from "../../../enum/SubscriptionStatusEnum";

export interface SubscriptionDto {
  id: number;
  // What the member asked for. Undefined when an administrator added it himself.
  requestedAmount?: number;
  // What counts towards the balance. Zero until approved.
  amount: number;
  status: SubscriptionStatusEnum;
  memberComment: string;
  adminComment: string;
  decidedOn?: string;
  userId: string;
  user?: UserDto;
  createdOn: string;
}

export class SubscriptionDto implements SubscriptionDto {
  id: number = 0;
  requestedAmount?: number = undefined;
  amount: number = 0;
  status: SubscriptionStatusEnum = SubscriptionStatusEnum.PENDING;
  memberComment: string = "";
  adminComment: string = "";
  decidedOn?: string = undefined;
  userId: string = "";
  user?: UserDto = undefined;
  createdOn: string = "";
}
