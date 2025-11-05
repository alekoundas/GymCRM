export interface ExerciseDto {
  id: number;
  name: string;
  description: string;
  sets: number;
  reps: number;
  weight: number;
  workoutPlanId: number;
  groupNumber: number;
  groupExerciseOrderNumber: number;
}

export class ExerciseDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: number = 1;
  reps: number = 1;
  weight: number = 1;
  workoutPlanId: number = 0;
  groupNumber: number = 0;
  groupExerciseOrderNumber: number = 0;
}
