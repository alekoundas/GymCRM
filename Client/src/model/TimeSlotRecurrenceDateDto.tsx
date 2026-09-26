import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

export interface TimeSlotRecurrenceDateDto {
  date: string; // UTC string
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum | undefined;
  // Which weekday (0 = Sunday) or day of the month a recurring date runs on. Read
  // these rather than date for a recurring entry: its date is a made-up day in 2000.
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  isUserJoined: boolean;
  trainGroupParticipantId: number | undefined; // used in Profile
  trainGroupParticipantUnavailableDateId: number | undefined; // used in Profile
  isOneOff: boolean; // used in Profile
  isUnavailableTrainGroup: boolean; // used in Profile
  // A session that actually took place, taken from the attendance rather than
  // projected from who is enrolled today. Read only - the date has been and gone.
  isAttendance: boolean;
  attendanceId: number | undefined;
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
  isAttendance: boolean = false;
  attendanceId: number | undefined;
}
