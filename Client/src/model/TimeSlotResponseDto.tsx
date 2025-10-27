import { UserDto } from "./entities/user/UserDto";
import { TimeSlotRecurrenceDateDto } from "./TimeSlotRecurrenceDateDto";

export interface TimeSlotResponseDto {
  id: number;
  title: string;
  description: string;
  trainerId: string;
  trainer: UserDto;
  trainGroupId: number;
  trainGroupDateId: number;
  duration: string;
  startOn: string;
  spotsLeft: number;
  isUnavailableTrainGroup: boolean;
  unavailableTrainGroupId: number | undefined;
  recurrenceDates: TimeSlotRecurrenceDateDto[];
}

export class TimeSlotResponseDto {
  id: number = 0;
  title: string = "";
  description: string = "";
  trainerId: string = "";
  trainer: UserDto = new UserDto();
  trainGroupId: number = -1;
  trainGroupDateId: number = -1;
  duration: string = "";
  startOn: string = "";
  spotsLeft: number = -1;
  isUnavailableTrainGroup: boolean = false;
  unavailableTrainGroupId: number | undefined;
  recurrenceDates: TimeSlotRecurrenceDateDto[] = [];
}
