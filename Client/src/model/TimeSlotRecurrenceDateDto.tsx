import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  trainGroupParticipantId: number | undefined;
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
  isUserJoined: boolean;
}

export class TimeSlotRecurrenceDateDto {
  trainGroupParticipantId: number | undefined;
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
  isUserJoined: boolean;
}
