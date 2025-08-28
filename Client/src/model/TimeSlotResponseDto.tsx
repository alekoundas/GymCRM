export interface TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: number;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  spotsLeft: number;
}

export class TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: number;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: Date;
  startOn: Date;
  spotsLeft: number;
}
