import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
  isUserJoined: boolean;
}

export class TimeSlotRecurrenceDateDto {
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
  isUserJoined: boolean;
}
