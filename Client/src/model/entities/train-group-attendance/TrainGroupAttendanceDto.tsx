import { UserDto } from "../user/UserDto";

export interface TrainGroupAttendanceDto {
  id: number;
  attendanceDate: string;
  trainGroupId: number;
  userId: string;
  user: UserDto;
}

export class TrainGroupAttendanceDto {
  id: number = 0;
  attendanceDate: string = "";
  trainGroupId: number = 0;
  userId: string = "";
  user: UserDto = new UserDto();
}
