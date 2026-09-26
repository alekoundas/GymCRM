import { UserDto } from "../user/UserDto";
export interface ExerciseDto {
  id: number;
  name: string;
  description: string;
  sets: string;
  reps: string;
  weight: string;
  videoUrl: string;
  groupNumber: number;
  groupExerciseOrderNumber: number;
  workoutPlanId: number;
  // Filled for the admin's exercise history page.
  workoutPlanTitle?: string;
  userId?: string;
  user?: UserDto;
  isWorkoutPlanInactive?: boolean;
}

export class ExerciseDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: string = "";
  reps: string = "";
  weight: string = "";
  videoUrl: string = "";
  groupNumber: number = 0;
  groupExerciseOrderNumber: number = 0;
  workoutPlanId: number = 0;
}
