// Interface
export interface WorkoutPlanRuleWeekDto {
  id?: number;
  weekNumber: number;
  message: string;
  maxRecordings: number;
  workoutPlanRuleId?: number;
}

// Class
export class WorkoutPlanRuleWeekDto implements WorkoutPlanRuleWeekDto {
  id?: number = undefined;
  weekNumber: number = 1;
  message: string = "";
  maxRecordings: number = 1;
  workoutPlanRuleId?: number = undefined;
}
