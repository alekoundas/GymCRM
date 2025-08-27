export interface TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: number;
  trainGroupId: number;
  trainGroupDateId: number;
  spotsLeft: number;
  duration: Date;
  startOn: Date;
  isAvailable: boolean;
}

export class TimeSlotResponseDto {
  title: string;
  description: string;
  trainerId: number;
  trainGroupId: number;
  trainGroupDateId: number;
  spotsLeft: number;
  duration: Date;
  startOn: Date;
  isAvailable: boolean;
}
