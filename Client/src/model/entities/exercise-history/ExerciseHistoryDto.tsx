export interface ExerciseHistoryDto {
  id: number;
  name: string;
  description: string;
  sets: number;
  reps: number;
  weight: number;
  groupNumber: number;
  groupExerciseOrderNumber: number;
  exerciseId: number;
  createdOn: string;
}

export class ExerciseHistoryDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: number = 1;
  reps: number = 1;
  weight: number = 1;
  groupNumber: number = 0;
  groupExerciseOrderNumber: number = 0;
  exerciseId: number = 0;
  createdOn: string = "";
}
