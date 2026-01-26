import { TrainGroupDto } from "../train-group/TrainGroupDto";
import { UserDto } from "../user/UserDto";

export interface TrainGroupAttendanceDto {
  id: number;
  attendanceDate: string;
  trainGroupId: number;
  trainGroup: TrainGroupDto;
  userId: string;
  user: UserDto;
}

export class TrainGroupAttendanceDto {
  id: number = 0;
  attendanceDate: string = "";
  trainGroupId: number = 0;
  trainGroup: TrainGroupDto = new TrainGroupDto();
  userId: string = "";
  user: UserDto = new UserDto();
}
