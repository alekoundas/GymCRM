import { TimeSlotRecurrenceDateDto } from "./TimeSlotRecurrenceDateDto";

export interface TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: string;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  spotsLeft: number;
  recurrenceDates: TimeSlotRecurrenceDateDto[];
}

export class TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: string;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  spotsLeft: number;
  recurrenceDates: TimeSlotRecurrenceDateDto[];
}
