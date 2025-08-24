export interface TimeSlotResponseDto {
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  displayDate: string;
  available: boolean;
}

export class TimeSlotResponseDto {
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  displayDate: string;
  available: boolean;
}
