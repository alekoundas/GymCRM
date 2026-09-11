export interface SubscriptionAddDto {
  userId: string;
  amount: number;
  adminComment: string;
  notifyUser: boolean;
}

export class SubscriptionAddDto implements SubscriptionAddDto {
  userId: string = "";
  amount: number = 0;
  adminComment: string = "";
  notifyUser: boolean = true;
}
