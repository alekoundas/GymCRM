import { create } from "zustand";
import { TrainGroupDateDto } from "../model/TrainGroupDateDto";
import { TrainGroupDto } from "../model/TrainGroupDto";
import ApiService from "../services/ApiService";
import { TrainGroupParticipantDto } from "../model/TrainGroupParticipantDto";
import { TokenService } from "../services/TokenService";
import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";

interface TrainGroupStoreState {
  trainGroupDto: TrainGroupDto;
  trainGroupDateDto: TrainGroupDateDto;
  trainGroupParticipant: TrainGroupParticipantDto;
  selectedTrainGroupDate: TrainGroupDateDto | undefined;

  setTrainGroupDto: (data: TrainGroupDto) => void;
  updateTrainGroupDto: (updates: Partial<TrainGroupDto>) => void;
  resetTrainGroupDto: (id?: number) => Promise<any>;

  setTrainGroupParticipant: (data: TrainGroupParticipantDto) => void;
  resetTrainGroupParticipant: () => Promise<any>;

  setTrainGroupDateDto: (data: TrainGroupDateDto) => void;
  resetTrainGroupDateDto: () => Promise<any>;

  setSelectedTrainGroupDate: (data: TrainGroupDateDto) => void;
  resetSelectedTrainGroupDate: () => Promise<any>;

  addTrainGroupDate: (newRow: TrainGroupDateDto) => void;
  addTrainGroupParticipant: (newRow: TrainGroupParticipantDto) => void;

  addTrainGroupDateParticipant: (newRow: TrainGroupParticipantDto) => void;
  editTrainGroupDateParticipant: (newRow: TrainGroupParticipantDto) => void;
  deleteTrainGroupDateParticipant: (
    participantId: number,
    trainGroupDateId: number
  ) => void;
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
  trainGroupDateDto: {
    id: 0,
    fixedDay: undefined,
    recurrenceDayOfMonth: undefined,
    recurrenceDayOfWeek: undefined,
    trainGroup: undefined,
    trainGroupDateType: TrainGroupDateTypeEnum.FIXED_DAY,
    trainGroupId: undefined,
    trainGroupParticipants: [],
    trainGroupDateCancellationSubscribers: [],
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

  setTrainGroupDateDto: (data: TrainGroupDateDto) =>
    set({ trainGroupDateDto: data }),
  resetTrainGroupDateDto: async () => {
    set({
      trainGroupDateDto: {
        id: 0,
        fixedDay: undefined,
        recurrenceDayOfMonth: undefined,
        recurrenceDayOfWeek: undefined,
        trainGroup: undefined,
        trainGroupDateType: TrainGroupDateTypeEnum.FIXED_DAY,
        trainGroupId: undefined,
        trainGroupParticipants: [],
        trainGroupDateCancellationSubscribers: [],
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

  deleteTrainGroupDateParticipant: (
    participantId: number,
    trainGroupDateId: number
  ) =>
    set((state) => {
      // Find the TrainGroupDateDto to update
      const targetDate = state.trainGroupDto.trainGroupDates.find(
        (x) => x.id === trainGroupDateId
      );

      if (!targetDate) {
        // If the date is not found, return the current state unchanged
        return state;
      }

      // Create updated TrainGroupDateDto with the participant removed
      const updatedDate = {
        ...targetDate,
        trainGroupParticipants: targetDate.trainGroupParticipants.filter(
          (participant) => participant.id !== participantId
        ),
      };

      // Update the trainGroupDates array
      const updatedDates = [
        ...state.trainGroupDto.trainGroupDates.filter(
          (x) => x.id !== trainGroupDateId
        ),
        updatedDate,
      ];

      // Update the trainGroupDto with the new dates array
      const newTrainGroupDto = {
        ...state.trainGroupDto,
        trainGroupDates: updatedDates,
      };

      // Update selectedTrainGroupDate if it matches the modified trainGroupDateId
      let newSelected = state.selectedTrainGroupDate;
      if (state.selectedTrainGroupDate?.id === trainGroupDateId) {
        newSelected = updatedDates.find((x) => x.id === trainGroupDateId);
      }

      return {
        trainGroupDto: newTrainGroupDto,
        selectedTrainGroupDate: newSelected,
      };
    }),

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
