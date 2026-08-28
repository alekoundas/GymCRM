// Mirrors Core.Enums.MailStatusEnum. The API serialises enums as strings
// (JsonStringEnumConverter), so these names must match exactly.
export enum MailStatusEnum {
  PENDING = "PENDING",
  SENT = "SENT",
  FAILED = "FAILED",
}
