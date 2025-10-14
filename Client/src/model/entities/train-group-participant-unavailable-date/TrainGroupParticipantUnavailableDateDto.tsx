export interface TrainGroupParticipantUnavailableDateDto {
  id: number;
  unavailableDate: string;
  trainGroupParticipantId: number;
}

export class TrainGroupParticipantUnavailableDateDto {
  id: number = -1;
  unavailableDate: string = "";
  trainGroupParticipantId: number = 0;
}
