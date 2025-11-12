import { ExerciseDto } from "../exercise/ExerciseDto";
import { UserDto } from "../user/UserDto";

export interface WorkoutPlanDto {
  id: number;
  title: string;
  description: string;
  isCircular: boolean;
  userId: string;
  user: UserDto;
  exercises: ExerciseDto[];
  createdOn: string;
}

export class WorkoutPlanDto {
  id: number = 0;
  title: string = "";
  description: string = "";
  isCircular: boolean = false;
  userId: string = "";
  user: UserDto = new UserDto();
  exercises: ExerciseDto[] = [];
  createdOn: string = "";
}
