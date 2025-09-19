import { TimeSlotRecurrenceDateDto } from "./TimeSlotRecurrenceDateDto";

export interface TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: string;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: string;
  startOn: string;
  spotsLeft: number;
  recurrenceDates: TimeSlotRecurrenceDateDto[];
}

export class TimeSlotResponseDto {
  title: string = "";
  description: string = "";
  trainerId: string = "";
  trainGroupId: number = -1;
  trainGroupDateId: number = -1;
  duration: string = "";
  startOn: string = "";
  spotsLeft: number = -1;
  recurrenceDates: TimeSlotRecurrenceDateDto[] = [];
}
