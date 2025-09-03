import { create } from "zustand";
import { TrainGroupDateDto } from "../model/TrainGroupDateDto";
import { TrainGroupDto } from "../model/TrainGroupDto";
import ApiService from "../services/ApiService";
import { TrainGroupParticipantDto } from "../model/TrainGroupParticipantDto";
import { TokenService } from "../services/TokenService";

interface TrainGroupStoreState {
  trainGroupDto: TrainGroupDto;
  trainGroupParticipant: TrainGroupParticipantDto;
  selectedTrainGroupDate: TrainGroupDateDto | undefined;

  setTrainGroupDto: (data: TrainGroupDto) => void;
  updateTrainGroupDto: (updates: Partial<TrainGroupDto>) => void;
  resetTrainGroupDto: (id?: number) => Promise<any>;

  setTrainGroupParticipant: (data: TrainGroupParticipantDto) => void;
  resetTrainGroupParticipant: () => Promise<any>;

  setSelectedTrainGroupDate: (data: TrainGroupDateDto) => void;
  resetSelectedTrainGroupDate: () => Promise<any>;

  addTrainGroupDate: (newRow: TrainGroupDateDto) => void;
  addTrainGroupParticipant: (newRow: TrainGroupParticipantDto) => void;

  addTrainGroupDateParticipant: (newRow: TrainGroupParticipantDto) => void;
  editTrainGroupDateParticipant: (newRow: TrainGroupParticipantDto) => void;
  resetTrainGroupDateParticipant: (id: number) => void;
}

export const useTrainGroupStore = create<TrainGroupStoreState>((set) => ({
  trainGroupDto: {
    id: 0,
    title: "",
    description: "",
    duration: new Date(2000, 0, 1, 0, 0, 0),
    startOn: new Date(2000, 0, 1, 0, 0, 0),
    maxParticipants: 1,
    trainerId: "",
    trainGroupDates: [],
    trainGroupParticipants: [],
  },
  trainGroupParticipant: {
    id: 0,
    selectedDate: undefined,
    trainGroupId: 0,
    trainGroupDateId: undefined,
    userId: TokenService.getUserId() ?? "",
  },
  selectedTrainGroupDate: undefined,
  setTrainGroupDto: (data) => set({ trainGroupDto: data }),
  updateTrainGroupDto: (updates) =>
    set((state) => ({
      trainGroupDto: { ...state.trainGroupDto, ...updates },
    })),
  resetTrainGroupDto: async (id?: number) => {
    if (id)
      return await ApiService.get<TrainGroupDto>("TrainGroups", id).then(
        (value) =>
          value
            ? set((state) => ({
                trainGroupDto: {
                  ...value,
                  trainGroupParticipants:
                    state.trainGroupDto.trainGroupParticipants,
                  trainGroupDates: state.trainGroupDto.trainGroupDates,
                },
              }))
            : null
      );
    else
      set({
        trainGroupDto: {
          id: 0,
          title: "",
          description: "",
          duration: new Date(2000, 0, 1, 0, 0, 0),
          startOn: new Date(2000, 0, 1, 0, 0, 0),
          maxParticipants: 1,
          trainerId: "",
          trainGroupDates: [],
          trainGroupParticipants: [],
        },
      });
  },

  setTrainGroupParticipant: (data: TrainGroupParticipantDto) =>
    set({ trainGroupParticipant: data }),
  resetTrainGroupParticipant: async () => {
    set({
      trainGroupParticipant: {
        id: 0,
        selectedDate: undefined,
        trainGroupId: 0,
        trainGroupDateId: undefined,
        userId: TokenService.getUserId() ?? "",
      },
    });
  },

  setSelectedTrainGroupDate: (data: TrainGroupDateDto) =>
    set({ selectedTrainGroupDate: data }),
  resetSelectedTrainGroupDate: async () => {
    set({
      selectedTrainGroupDate: undefined,
    });
  },

  addTrainGroupDate: (newRow) =>
    set((state) => ({
      trainGroupDto: {
        ...state.trainGroupDto,
        trainGroupDates: [...state.trainGroupDto.trainGroupDates, newRow],
      },
    })),

  addTrainGroupParticipant: (newRow) =>
    set((state) => ({
      trainGroupDto: {
        ...state.trainGroupDto,
        trainGroupParticipants: [
          ...state.trainGroupDto.trainGroupParticipants,
          newRow,
        ],
      },
    })),

  addTrainGroupDateParticipant: (newRow: TrainGroupParticipantDto) =>
    set((state) => {
      const updatedDate = {
        ...state.trainGroupDto.trainGroupDates.find(
          (x) => x.id === newRow.trainGroupDateId
        )!,
        trainGroupParticipants: [
          ...state.trainGroupDto.trainGroupDates.find(
            (x) => x.id === newRow.trainGroupDateId
          )!.trainGroupParticipants,
          newRow,
        ],
      };

      const updatedDates = [
        ...state.trainGroupDto.trainGroupDates.filter(
          (x) => x.id !== newRow.trainGroupDateId
        ),
        updatedDate,
      ];

      const newTrainGroupDto = {
        ...state.trainGroupDto,
        trainGroupDates: updatedDates,
      };

      let newSelected = state.selectedTrainGroupDate;
      if (state.selectedTrainGroupDate?.id === newRow.trainGroupDateId) {
        newSelected = updatedDates.find(
          (x) => x.id === newRow.trainGroupDateId
        );
      }

      return {
        trainGroupDto: newTrainGroupDto,
        selectedTrainGroupDate: newSelected,
      };
    }),

  editTrainGroupDateParticipant: (row: TrainGroupParticipantDto) =>
    set((state) => ({
      trainGroupDto: {
        ...state.trainGroupDto,
        trainGroupDates: [
          ...state.trainGroupDto.trainGroupDates.filter(
            (x) => x.id !== row.trainGroupDateId
          ),
          {
            ...state.trainGroupDto.trainGroupDates.filter(
              (x) => x.id === row.trainGroupDateId
            )[0],
            trainGroupParticipants: [
              ...state.trainGroupDto.trainGroupDates
                .filter((x) => x.id === row.trainGroupDateId)[0]
                .trainGroupParticipants.filter((x) => x.id !== row.id),
              row,
            ],
          },
        ],
      },
    })),

  resetTrainGroupDateParticipant: (id: number) =>
    set((state) => ({
      trainGroupDto: {
        ...state.trainGroupDto,
        trainGroupDates: [
          ...state.trainGroupDto.trainGroupDates.filter((x) => x.id !== id),
          {
            ...state.trainGroupDto.trainGroupDates.filter(
              (x) => x.id === id
            )[0],
            trainGroupParticipants: [],
          },
        ],
      },
    })),
}));
