import { create } from "zustand";
import { TrainGroupDateDto } from "../model/TrainGroupDateDto";
import { TrainGroupDto } from "../model/TrainGroupDto";
import ApiService from "../services/ApiService";

interface TrainGroupStoreState {
  trainGroupDto: TrainGroupDto;
  setTrainGroupDto: (data: TrainGroupDto) => void;
  updateTrainGroupDto: (updates: Partial<TrainGroupDto>) => void;
  addTrainGroupDate: (newRow: TrainGroupDateDto) => void;
  resetTrainGroupDto: (id?: number) => Promise<any>;
}

export const useTrainGroupStore = create<TrainGroupStoreState>((set) => ({
  trainGroupDto: {
    id: -1,
    title: "",
    description: "",
    isRepeating: false,
    duration: new Date(2000, 0, 1, 0, 0, 0),
    startOn: new Date(2000, 0, 1, 0, 0, 0),
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
  resetTrainGroupDto: async (id?: number) => {
    if (id)
      return await ApiService.get<TrainGroupDto>("TrainGroups", id).then(
        (value) => (value ? set({ trainGroupDto: value }) : null)
      );
    else
      set({
        trainGroupDto: {
          id: -1,
          title: "",
          description: "",
          duration: new Date(2000, 0, 1, 0, 0, 0),
          startOn: new Date(2000, 0, 1, 0, 0, 0),
          maxParticipants: 1,
          trainerId: "",
          repeatingParticipants: [],
          trainGroupDates: [],
        },
      });
  },
}));
