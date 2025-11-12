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
