import { WorkoutPlanStartScenario } from "../../../enum/WorkoutPlanStartScenario";
import { WorkoutPlanRecordingDto } from "./WorkoutPlanRecordingDto";

// Interface
export interface WorkoutPlanStartContextDto {
  workoutPlanId: number;
  scenario: WorkoutPlanStartScenario;
  hasRule: boolean;
  ruleName: string;
  weekCount: number;
  currentWeek?: number;
  currentWeekMessage: string;
  nextWeek: number;
  recordingsOnCurrentWeek: number;
  maxRecordings: number;
  remainingOnCurrentWeek: number;
  lastRecordingOn?: string;
  daysSinceLastRecording?: number;
  availableWeeks: number[];
  runningRecording?: WorkoutPlanRecordingDto;
}

// Class
export class WorkoutPlanStartContextDto implements WorkoutPlanStartContextDto {
  workoutPlanId: number = 0;
  scenario: WorkoutPlanStartScenario = WorkoutPlanStartScenario.NoRule;
  hasRule: boolean = false;
  ruleName: string = "";
  weekCount: number = 0;
  currentWeek?: number = undefined;
  currentWeekMessage: string = "";
  nextWeek: number = 1;
  recordingsOnCurrentWeek: number = 0;
  maxRecordings: number = 0;
  remainingOnCurrentWeek: number = 0;
  lastRecordingOn?: string = undefined;
  daysSinceLastRecording?: number = undefined;
  availableWeeks: number[] = [];
  runningRecording?: WorkoutPlanRecordingDto = undefined;
}
