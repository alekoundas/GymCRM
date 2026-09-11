// Mirrors Core.Enums.SubscriptionStatusEnum. The API serialises enums as strings
// (JsonStringEnumConverter), so these names must match exactly.
export enum SubscriptionStatusEnum {
  PENDING = "PENDING",
  APPROVED = "APPROVED",
  REJECTED = "REJECTED",
}
