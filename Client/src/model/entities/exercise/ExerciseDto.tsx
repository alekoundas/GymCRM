export interface ExerciseDto {
  id: number;
  name: string;
  description: string;
  sets: number;
  reps: number;
  weight: number;
}

export class ExerciseDto {
  id: number = 0;
  name: string = "";
  description: string = "";
  sets: number = 0;
  reps: number = 0;
  weight: number = 0;
}
