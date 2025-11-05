import { create } from "zustand";
import { WorkoutPlanDto } from "../model/entities/workout-plan/WorkoutPlanDto";
import { ExerciseDto } from "../model/entities/exercise/ExerciseDto";
import { ExerciseHistoryDto } from "../model/entities/exercise-history/ExerciseHistoryDto";

interface WorkoutPlanStoreState {
  workoutPlanDto: WorkoutPlanDto;
  newExerciseDto: ExerciseDto;
  exerciseHistoryDto: ExerciseHistoryDto;

  setWorkoutPlanDto: (data: WorkoutPlanDto) => void;
  updateWorkoutPlanDto: (updates: Partial<WorkoutPlanDto>) => void;
  resetWorkoutPlanDto: () => void;
  setExercises: (data: ExerciseDto[]) => void;

  setNewExerciseDto: (data: ExerciseDto) => void;
  updateNewExerciseDto: (updates: Partial<ExerciseDto>) => void;
  resetNewExerciseDto: () => void;

  setExerciseHistoryDto: (data: ExerciseHistoryDto) => void;
}

export const useWorkoutPlanStore = create<WorkoutPlanStoreState>((set) => ({
  workoutPlanDto: new WorkoutPlanDto(),
  newExerciseDto: new ExerciseDto(),
  exerciseHistoryDto: new ExerciseHistoryDto(),

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
  setExercises: (data: ExerciseDto[]) =>
    set((state) => ({
      workoutPlanDto: { ...state.workoutPlanDto, exercises: data },
    })),

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

  // exerciseHistoryDtos
  setExerciseHistoryDto: (data: ExerciseHistoryDto) =>
    set({ exerciseHistoryDto: data }),
}));
