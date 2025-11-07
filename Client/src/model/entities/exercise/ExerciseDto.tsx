export interface ExerciseDto {
  id: number;
  name: string;
  description: string;
  sets: string;
  videoUrl: string;
  reps: number;
  weight: number;
  groupNumber: number;
  groupExerciseOrderNumber: number;
  workoutPlanId: number;
}

export class ExerciseDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: string = "";
  videoUrl: string = "";
  reps: number = 1;
  weight: number = 1;
  groupNumber: number = 0;
  groupExerciseOrderNumber: number = 0;
  workoutPlanId: number = 0;
}
