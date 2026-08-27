import { UserDto } from "../user/UserDto";

// Interface
export interface WorkoutPlanRecordingDto {
  id?: number;
  startedOn: string;
  completedOn?: string;
  durationSeconds?: number;
  weekNumber: number;
  workoutPlanId?: number;
  workoutPlanTitle: string;
  userId: string;
  user?: UserDto;
  isRunning: boolean;
  isIncomplete: boolean;
  // Server-computed. Never derive elapsed time from startedOn on the client.
  elapsedSeconds?: number;
  createdOn?: string;
}

// Class
export class WorkoutPlanRecordingDto implements WorkoutPlanRecordingDto {
  id?: number = undefined;
  startedOn: string = "";
  completedOn?: string = undefined;
  durationSeconds?: number = undefined;
  weekNumber: number = 0;
  workoutPlanId?: number = undefined;
  workoutPlanTitle: string = "";
  userId: string = "";
  user?: UserDto = undefined;
  isRunning: boolean = false;
  isIncomplete: boolean = false;
  elapsedSeconds?: number = undefined;
  createdOn?: string = undefined;
}
