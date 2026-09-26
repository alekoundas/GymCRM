export interface SubscriptionMonthDto {
  // Plain numbers, not a date: a date would arrive shifted to UTC and a January
  // boundary would read as December for anybody west of here.
  year: number;
  month: number;
  amount: number;
}

export interface SubscriptionBucketDto {
  // A band, not a label - the text is picked here so it can be translated.
  key: "ONE_OR_LESS" | "TWO_TO_FIVE" | "SIX_TO_TEN" | "ELEVEN_PLUS";
  count: number;
}

export interface SubscriptionDebtorDto {
  userId: string;
  fullName: string;
  balance: number;
}

export interface SubscriptionPendingRequestDto {
  id: number;
  userId: string;
  fullName: string;
  requestedAmount: number;
  // Days waited, not a date: no timezone gets a say in whether it reads as one or two.
  waitingDays: number;
}

export interface SubscriptionChartsDto {
  monthlyApproved: SubscriptionMonthDto[];
  buckets: SubscriptionBucketDto[];
  topDebtors: SubscriptionDebtorDto[];
  oldestPendingRequests: SubscriptionPendingRequestDto[];
}
