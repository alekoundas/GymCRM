export interface SubscriptionMonthDto {
  // The first of the month; formatted client side so the label follows the language.
  month: string;
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

export interface SubscriptionChartsDto {
  monthlyApproved: SubscriptionMonthDto[];
  buckets: SubscriptionBucketDto[];
  topDebtors: SubscriptionDebtorDto[];
}
