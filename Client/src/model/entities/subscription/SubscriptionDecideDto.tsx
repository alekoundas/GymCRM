export interface SubscriptionDecideDto {
  id: number;
  isApproved: boolean;
  // What is actually granted, which need not be what was asked for.
  amount: number;
  adminComment: string;
  notifyUser: boolean;
}

export class SubscriptionDecideDto implements SubscriptionDecideDto {
  id: number = 0;
  isApproved: boolean = true;
  amount: number = 0;
  adminComment: string = "";
  notifyUser: boolean = false;
}
