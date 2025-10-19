import { ExerciseDto } from "../exercise/ExerciseDto";
import { UserDto } from "../user/UserDto";

export interface WorkoutPlanDto {
  id: number;
  title: string;
  userId: string;
  user: UserDto;
  exercises: ExerciseDto[];
}

export class WorkoutPlanDto {
  id: number = 0;
  title: string = "";
  userId: string = "";
  user: UserDto = new UserDto();
  exercises: ExerciseDto[] = [];
}
