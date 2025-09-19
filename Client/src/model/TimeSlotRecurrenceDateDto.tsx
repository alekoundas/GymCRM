import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  date: string; // UTC string
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum | undefined;
  isUserJoined: boolean;
  trainGroupParticipantId: number | undefined; // used as id in html
}

export class TimeSlotRecurrenceDateDto {
  date: string = ""; // UTC string
  trainGroupDateId: number = -1;
  trainGroupDateType: TrainGroupDateTypeEnum | undefined;
  isUserJoined: boolean = false;
  trainGroupParticipantId: number | undefined; // used as id in html
}
