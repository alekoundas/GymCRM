export interface TrainGroupCancellationSubscriberDto {
  id: string; // Required, non-empty string
  selectedDate: string; // Required, ISO 8601 date (e.g., "2025-08-23T09:00:00Z")
  trainGroupDateId: number; // Required
  cancellationSubscriberId: string; // Required, valid GUID
}

export class TrainGroupCancellationSubscriberDto {
  id: string = "";
  selectedDate: string = "";
  trainGroupDateId: number = 0;
  cancellationSubscriberId: string = "";
}
