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

  // Null until an admin assigns one - Start stays disabled while null.
  workoutPlanRuleId?: number;
  workoutPlanRuleName: string;
  // Plain week number, not an id.
  currentWeek?: number;
  weekCount: number;
  currentWeekMessage: string;

  isRunning: boolean;
  // Server-computed - never derive elapsed time from a timestamp here.
  elapsedSeconds?: number;
  hasIncompleteRecording: boolean;
  lastRecordingOn?: string;
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

  workoutPlanRuleId?: number = undefined;
  workoutPlanRuleName: string = "";
  currentWeek?: number = undefined;
  weekCount: number = 0;
  currentWeekMessage: string = "";

  isRunning: boolean = false;
  elapsedSeconds?: number = undefined;
  hasIncompleteRecording: boolean = false;
  lastRecordingOn?: string = undefined;
}
