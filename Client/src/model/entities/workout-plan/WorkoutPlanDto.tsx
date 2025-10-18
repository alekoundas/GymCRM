import { ExerciseDto } from "../exercise/ExerciseDto";
import { UserDto } from "../user/UserDto";

export interface WorkoutPlanDto {
  id: number;
  title: string;
  userId: number;
  user: UserDto;
  exercises: ExerciseDto[];
}

export class WorkoutPlanDto {
  id: number = 0;
  title: string = "";
  userId: number = 0;
  user: UserDto = new UserDto();
  exercises: ExerciseDto[] = [];
}
