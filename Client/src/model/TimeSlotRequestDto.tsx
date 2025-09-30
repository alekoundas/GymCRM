import { TokenService } from "../services/TokenService";

export interface TimeSlotRequestDto {
  selectedDate: string;
  userId: string;
}

export class TimeSlotRequestDto {
  selectedDate: string = "";
  userId: string = TokenService.getUserId() ?? "";
}
