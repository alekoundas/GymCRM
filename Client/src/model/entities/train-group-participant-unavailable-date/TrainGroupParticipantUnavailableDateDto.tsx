import { UserDto } from "../user/UserDto";

export interface TrainGroupParticipantUnavailableDateDto {
  id: number;
  unavailableDate: Date;
  trainGroupParticipantId: number;
}

export class TrainGroupParticipantUnavailableDateDto {
  id: number = -1;
  unavailableDate: Date = new Date();
  trainGroupParticipantId: number = 0;
}
