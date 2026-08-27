import { create } from "zustand";
import { WorkoutPlanRuleDto } from "../model/entities/workout-plan-rule/WorkoutPlanRuleDto";

interface WorkoutPlanRuleStoreState {
  workoutPlanRuleDto: WorkoutPlanRuleDto;

  setWorkoutPlanRuleDto: (data: WorkoutPlanRuleDto) => void;
  updateWorkoutPlanRuleDto: (updates: Partial<WorkoutPlanRuleDto>) => void;
  resetWorkoutPlanRuleDto: () => void;
}

export const useWorkoutPlanRuleStore = create<WorkoutPlanRuleStoreState>(
  (set) => ({
    workoutPlanRuleDto: new WorkoutPlanRuleDto(),

    setWorkoutPlanRuleDto: (data) => set({ workoutPlanRuleDto: data }),
    updateWorkoutPlanRuleDto: (updates) =>
      set((state) => ({
        workoutPlanRuleDto: { ...state.workoutPlanRuleDto, ...updates },
      })),
    resetWorkoutPlanRuleDto: () =>
      set({ workoutPlanRuleDto: { ...new WorkoutPlanRuleDto() } }),
  }),
);
