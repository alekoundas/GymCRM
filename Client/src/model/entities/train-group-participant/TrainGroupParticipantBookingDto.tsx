import { TrainGroupDateTypeEnum } from "../../../enum/TrainGroupDateTypeEnum";

// One press of Book: just the picked session, and/or every one of the ticked days of
// the group from that day on.
export interface TrainGroupParticipantBookDto {
  trainGroupId: number;
  selectedDate: string;
  isOneOff: boolean;
  recurringTrainGroupDateIds: number[];
  // Staff only. Left out, the booking is the caller's own.
  userId?: string;
  clientTimezoneOffsetMinutes: number;
}

// Ending a recurring booking from a date, or cancelling a one-off.
export interface TrainGroupParticipantRemoveDto {
  fromDate?: string;
  clientTimezoneOffsetMinutes: number;
  isAdminPage: boolean;
}

export interface TrainGroupParticipantSkipDto {
  id: number;
  date: string;
}

// A booking as the booking page lists it. Every date here is midnight UTC and is read
// with the UTC getters.
export interface TrainGroupParticipantBookingDto {
  id: number;
  trainGroupId: number;
  trainGroupDateId: number;
  trainGroupDateType: TrainGroupDateTypeEnum;
  title: string;
  trainerFullName: string;
  startOn: string;
  duration: string;
  isOneOff: boolean;
  date?: string;
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  startOnDate?: string;
  // The first date no longer covered.
  endOnDate?: string;
  removedOn?: string;
  removedBy_FullName: string;
  nextSessionDate?: string;
  lastSessionDate?: string;
  upcomingSkips: TrainGroupParticipantSkipDto[];
}
