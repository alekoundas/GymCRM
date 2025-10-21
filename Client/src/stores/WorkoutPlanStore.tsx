import { create } from "zustand";
import { WorkoutPlanDto } from "../model/entities/workout-plan/WorkoutPlanDto";
import { ExerciseDto } from "../model/entities/exercise/ExerciseDto";

interface WorkoutPlanStoreState {
  workoutPlanDto: WorkoutPlanDto;
  newExerciseDto: ExerciseDto;

  setWorkoutPlanDto: (data: WorkoutPlanDto) => void;
  updateWorkoutPlanDto: (updates: Partial<WorkoutPlanDto>) => void;
  resetWorkoutPlanDto: () => void;

  setNewExerciseDto: (data: ExerciseDto) => void;
  updateNewExerciseDto: (updates: Partial<ExerciseDto>) => void;
  resetNewExerciseDto: () => void;

  setExercises: (data: ExerciseDto[]) => void;
  // resetExercises: () => void;
}

export const useWorkoutPlanStore = create<WorkoutPlanStoreState>((set) => ({
  workoutPlanDto: new WorkoutPlanDto(),
  newExerciseDto: new ExerciseDto(),

  // workoutPlanDto
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

  // newExerciseDto
  setNewExerciseDto: (data) => set({ newExerciseDto: data }),
  updateNewExerciseDto: (updates) =>
    set((state) => ({
      newExerciseDto: { ...state.newExerciseDto, ...updates },
    })),
  resetNewExerciseDto: () => {
    set({
      newExerciseDto: {
        ...new ExerciseDto(),
      },
    });
  },

  setExercises: (data: ExerciseDto[]) =>
    set((state) => ({
      workoutPlanDto: { ...state.workoutPlanDto, exercises: data },
    })),
}));
