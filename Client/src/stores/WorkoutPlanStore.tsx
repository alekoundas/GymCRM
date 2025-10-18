import { create } from "zustand";
import { WorkoutPlanDto } from "../model/entities/workout-plan/WorkoutPlanDto";

interface WorkoutPlanStoreState {
  workoutPlanDto: WorkoutPlanDto;
  setWorkoutPlanDto: (data: WorkoutPlanDto) => void;
  updateWorkoutPlanDto: (updates: Partial<WorkoutPlanDto>) => void;
  resetWorkoutPlanDto: () => void;
}

export const useWorkoutPlanStore = create<WorkoutPlanStoreState>((set) => ({
  workoutPlanDto: new WorkoutPlanDto(),
  setWorkoutPlanDto: (data) => set({ workoutPlanDto: data }),

  updateWorkoutPlanDto: (updates) =>
    set((state) => ({
      workoutPlanDto: { ...state.workoutPlanDto, ...updates },
    })),

  resetWorkoutPlanDto: () => {
    set({
      workoutPlanDto: {
        ...new WorkoutPlanDto(),
      },
    });
  },
}));
