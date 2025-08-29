import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
}

export class TimeSlotRecurrenceDateDto {
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  date: string; // UTC string
}
