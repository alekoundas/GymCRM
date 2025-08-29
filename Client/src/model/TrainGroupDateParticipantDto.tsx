export interface TrainGroupDateParticipantDto {
  selectedDate: string | null; // ISO 8601 string or null
  trainGroupDateId: number;
  userId: string | null; // GUID as string
}

export class TrainGroupDateParticipantDto {
  selectedDate: string | null; // ISO 8601 string or null
  trainGroupDateId: number;
  userId: string | null; // GUID as string
}
