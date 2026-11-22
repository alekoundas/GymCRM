import { WorkoutPlanRuleWeekDto } from "./WorkoutPlanRuleWeekDto";

// Same value as WorkoutPlanRecordingService.AwayGapDays on the server, which is
// what a rule saved without a number of days falls back to.
export const DEFAULT_AWAY_GAP_DAYS = 12;

// Interface
export interface WorkoutPlanRuleDto {
  id?: number;
  name: string;
  weeks: WorkoutPlanRuleWeekDto[];
  weekCount: number;
  workoutPlanCount: number;
  isLocked: boolean;
  createdOn?: string;
  // Required in the form. Rules saved before that can still have none, which
  // means DEFAULT_AWAY_GAP_DAYS.
  awayGapDays?: number | null;
}

// Class
export class WorkoutPlanRuleDto implements WorkoutPlanRuleDto {
  id?: number = undefined;
  name: string = "";
  weeks: WorkoutPlanRuleWeekDto[] = [];
  weekCount: number = 0;
  workoutPlanCount: number = 0;
  isLocked: boolean = false;
  createdOn?: string = undefined;
  awayGapDays?: number | null = DEFAULT_AWAY_GAP_DAYS;
}
