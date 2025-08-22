export interface TrainGroupParticipantDto {
  id: number;
  selectedDate: Date;
  trainGroupDateId: number;
  participantId: string;
}

export class TrainGroupParticipantDto {
  id: number;
  selectedDate: Date;
  trainGroupDateId: number = 0;
  participantId: string = "";
}
