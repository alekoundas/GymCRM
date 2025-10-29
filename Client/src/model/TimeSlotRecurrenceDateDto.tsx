import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  date: string; // UTC string
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum | undefined;
  isUserJoined: boolean;
  trainGroupParticipantId: number | undefined; // used in Profile
  trainGroupParticipantUnavailableDateId: number | undefined; // used in Profile
  isOneOff: boolean; // used in Profile
  isUnavailableTrainGroup: boolean; // used in Profile
}

export class TimeSlotRecurrenceDateDto {
  date: string = ""; // UTC string
  trainGroupDateId: number = -1;
  trainGroupDateType: TrainGroupDateTypeEnum | undefined;
  isUserJoined: boolean = false;
  trainGroupParticipantId: number | undefined; // used in Profile
  trainGroupParticipantUnavailableDateId: number | undefined; // used in Profile
  isOneOff: boolean = false; // used in Profile
  isUnavailableTrainGroup: boolean = false; // used in Profile
}
