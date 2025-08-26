export interface TimeSlotResponseDto {
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  available: boolean;
}

export class TimeSlotResponseDto {
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  available: boolean;
}
