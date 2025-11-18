import { create } from "zustand";
import { TrainGroupAttendanceDto } from "../model/entities/train-group-attendance/TrainGroupAttendanceDto";
import { TrainGroupParticipantDto } from "../model/entities/train-group-participant/TrainGroupParticipantDto";

interface TrainGroupAttendanceStoreState {
  trainGroupAttendanceDto: TrainGroupAttendanceDto;
  trainGroupParticipants: TrainGroupParticipantDto[];
  selectedUserIds: string[];

  setTrainGroupAttendanceDto: (data: TrainGroupAttendanceDto) => void;
  updateTrainGroupAttendanceDto: (
    updates: Partial<TrainGroupAttendanceDto>
  ) => void;
  resetTrainGroupAttendanceDto: () => void;

  // setTrainGroupAttendances: (data: TrainGroupAttendanceDto[]) => void;
  // resetTrainGroupAttendances: () => void;

  setTrainGroupParticipants: (data: TrainGroupParticipantDto[]) => void;
  resetTrainGroupParticipants: () => void;

  setSelectedUserIds: (data: string[]) => void;
  resetSelectedUserIds: () => void;
}

export const useTrainGroupAttendanceStore =
  create<TrainGroupAttendanceStoreState>((set) => ({
    trainGroupAttendanceDto: new TrainGroupAttendanceDto(),
    // trainGroupAttendances: [],
    trainGroupParticipants: [],
    selectedUserIds: [],

    setTrainGroupAttendanceDto: (data) =>
      set({ trainGroupAttendanceDto: data }),
    updateTrainGroupAttendanceDto: (updates) =>
      set((state) => ({
        trainGroupAttendanceDto: {
          ...state.trainGroupAttendanceDto,
          ...updates,
        },
      })),
    resetTrainGroupAttendanceDto: () => {
      set({
        trainGroupAttendanceDto: new TrainGroupAttendanceDto(),
      });
    },

    // setTrainGroupAttendances: (data: TrainGroupAttendanceDto[]) =>
    //   set({ trainGroupAttendances: data }),
    // resetTrainGroupAttendances: () => set({ trainGroupAttendances: [] }),

    setTrainGroupParticipants: (data: TrainGroupParticipantDto[]) =>
      set({ trainGroupParticipants: data }),
    resetTrainGroupParticipants: () => set({ trainGroupParticipants: [] }),

    setSelectedUserIds: (data: string[]) => set({ selectedUserIds: data }),
    resetSelectedUserIds: () => set({ selectedUserIds: [] }),
  }));
