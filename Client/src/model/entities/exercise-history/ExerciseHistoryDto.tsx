export interface ExerciseHistoryDto {
  id: number;
  name: string;
  description: string;
  sets: string;
  reps: string;
  weight: string;
  groupNumber: number;
  groupExerciseOrderNumber: number;
  exerciseId: number;
  createdOn: string;
}

export class ExerciseHistoryDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: string = "";
  reps: string = "";
  weight: string = "";
  groupNumber: number = 0;
  groupExerciseOrderNumber: number = 0;
  exerciseId: number = 0;
  createdOn: string = "";
}
