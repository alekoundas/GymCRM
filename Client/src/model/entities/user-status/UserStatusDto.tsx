import { ExerciseDto } from "../exercise/ExerciseDto";
import { UserDto } from "../user/UserDto";

export interface UserStatusDto {
  id: number;
  name: string;
  color: string;
  user: UserDto;
}

export class UserStatusDto {
  id: number = 0;
  name: string = "";
  color: string = "";
  user: UserDto = new UserDto();
}
