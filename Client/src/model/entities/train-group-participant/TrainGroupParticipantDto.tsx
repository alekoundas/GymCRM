import { UserDto } from "../user/UserDto";

export interface TrainGroupParticipantDto {
  id: number;
  selectedDate: string | undefined; // ISO 8601 string or null
  recurringStartOnDate?:string // Set only for recurring participants.
  trainGroupDateId: number | undefined;
  trainGroupId: number;
  userId: string; // GUID as string
  user?: UserDto | undefined;
  // Filled per request for the date the grid is filtered by.
  hasAttendance?: boolean;
}

export class TrainGroupParticipantDto {
  id: number = -1;
  selectedDate: string | undefined; // ISO 8601 string or null
  recurringStartOnDate?:string // Set only for recurring participants.
  trainGroupDateId: number | undefined;
  trainGroupId: number = -1;
  userId: string = ""; // GUID as string
  user?: UserDto | undefined;
  hasAttendance?: boolean = false;
}
