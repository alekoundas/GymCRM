import { create } from "zustand";
import { WorkoutPlanDto } from "../model/entities/workout-plan/WorkoutPlanDto";
import { ExerciseDto } from "../model/entities/exercise/ExerciseDto";

interface WorkoutPlanStoreState {
  workoutPlanDto: WorkoutPlanDto;
  setWorkoutPlanDto: (data: WorkoutPlanDto) => void;
  updateWorkoutPlanDto: (updates: Partial<WorkoutPlanDto>) => void;
  resetWorkoutPlanDto: () => void;

  setExercises: (data: ExerciseDto[]) => void;
  // resetExercises: () => void;
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

  setExercises: (data: ExerciseDto[]) =>
    set((state) => ({
      workoutPlanDto: { ...state.workoutPlanDto, exercises: data },
    })),
}));
