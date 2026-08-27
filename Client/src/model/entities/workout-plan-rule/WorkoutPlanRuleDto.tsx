import { WorkoutPlanRuleWeekDto } from "./WorkoutPlanRuleWeekDto";

// Interface
export interface WorkoutPlanRuleDto {
  id?: number;
  name: string;
  weeks: WorkoutPlanRuleWeekDto[];
  weekCount: number;
  workoutPlanCount: number;
  isLocked: boolean;
  createdOn?: string;
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
}
