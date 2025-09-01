export interface TrainGroupParticipantDto {
  id: number;
  selectedDate: string | undefined; // ISO 8601 string or null
  trainGroupDateId: number | undefined;
  trainGroupId: number;
  userId: string | undefined; // GUID as string
}

export class TrainGroupParticipantDto {
  id: number;
  selectedDate: string | undefined; // ISO 8601 string or null
  trainGroupDateId: number | undefined;
  trainGroupId: number;
  userId: string | undefined; // GUID as string
}
