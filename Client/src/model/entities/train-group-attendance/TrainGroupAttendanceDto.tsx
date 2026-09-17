import { TrainGroupDto } from "../train-group/TrainGroupDto";
import { UserDto } from "../user/UserDto";

export interface TrainGroupAttendanceDto {
  id: number;
  attendanceDate: string;

  // Null once the group is deleted; the snapshot below carries on.
  trainGroupId?: number;
  trainGroup?: TrainGroupDto;

  // What the session was, as it stood on the day.
  trainGroupTitle: string;
  trainGroupDescription: string;
  // Optional, so an object built here and posted leaves them out altogether rather
  // than sending an empty string where the server is expecting a date.
  trainGroupStartOn?: string;
  trainGroupDuration?: string;
  trainerFullName: string;
  trainerId?: string;

  createdOn: string;
  userId: string;
  user: UserDto;
}

export class TrainGroupAttendanceDto {
  id: number = 0;
  attendanceDate: string = "";
  trainGroupId?: number = undefined;
  trainGroup?: TrainGroupDto = undefined;
  trainGroupTitle: string = "";
  trainGroupDescription: string = "";
  trainGroupStartOn?: string = undefined;
  trainGroupDuration?: string = undefined;
  trainerFullName: string = "";
  trainerId?: string = undefined;
  createdOn: string = "";
  userId: string = "";
  user: UserDto = new UserDto();
}
