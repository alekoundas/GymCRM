import { create } from "zustand";
import { TrainGroupDateDto } from "../model/TrainGroupDateDto";
import { TrainGroupDto } from "../model/TrainGroupDto";

interface TrainGroupState {
  trainGroupDto: TrainGroupDto;
  setTrainGroupDto: (data: TrainGroupDto) => void;
  updateTrainGroupDto: (updates: Partial<TrainGroupDto>) => void;
  addTrainGroupDate: (newRow: TrainGroupDateDto) => void;
  resetTrainGroupDto: () => void;
}

export const useTrainGroupStore = create<TrainGroupState>((set) => ({
  trainGroupDto: {
    id: -1,
    name: "",
    description: "",
    isRepeating: false,
    duration: new Date(),
    startOn: new Date(),
    maxParticipants: 1,
    trainerId: "",
    repeatingParticipants: [],
    trainGroupDates: [],
  },
  setTrainGroupDto: (data) => set({ trainGroupDto: data }),
  updateTrainGroupDto: (updates) =>
    set((state) => ({
      trainGroupDto: { ...state.trainGroupDto, ...updates },
    })),
  addTrainGroupDate: (newRow) =>
    set((state) => ({
      trainGroupDto: {
        ...state.trainGroupDto,
        trainGroupDates: [...state.trainGroupDto.trainGroupDates, newRow],
      },
    })),
  resetTrainGroupDto: () =>
    set({
      trainGroupDto: {
        id: -1,
        name: "",
        description: "",
        duration: new Date(),
        startOn: new Date(),
        maxParticipants: 1,
        trainerId: "",
        repeatingParticipants: [],
        trainGroupDates: [],
      },
    }),
}));
