import { create } from "zustand";
import { TokenService } from "../services/TokenService";
import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";
import { TrainGroupDto } from "../model/entities/train-group/TrainGroupDto";
import { TrainGroupDateDto } from "../model/entities/train-group-date/TrainGroupDateDto";
import { TrainGroupParticipantDto } from "../model/entities/train-group-participant/TrainGroupParticipantDto";

interface TrainGroupStoreState {
  trainGroupDto: TrainGroupDto;
  trainGroupDateDto: TrainGroupDateDto;
  trainGroupParticipant: TrainGroupParticipantDto;
  selectedTrainGroupDate: TrainGroupDateDto | undefined;

  setTrainGroupDto: (data: TrainGroupDto) => void;
  updateTrainGroupDto: (updates: Partial<TrainGroupDto>) => void;
  resetTrainGroupDto: () => void;

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
  deleteTrainGroupDateParticipant: (row: TrainGroupParticipantDto) => void;
  resetTrainGroupDateParticipant: (id: number) => void;
}

export const useTrainGroupStore = create<TrainGroupStoreState>((set) => ({
  trainGroupDto: {
    id: 0,
    title: "",
    description: "",
    duration: "",
    startOn: "",
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
  resetTrainGroupDto: () => {
    //  if (id)
    //   return await ApiService()
    //     .get<TrainGroupDto>("TrainGroups", id)
    //     .then((value) =>
    //       value
    //         ? set((state) => ({
    //             trainGroupDto: {
    //               ...value,
    //               trainGroupParticipants:
    //                 state.trainGroupDto.trainGroupParticipants,
    //               trainGroupDates: state.trainGroupDto.trainGroupDates,
    //             },
    //           }))
    //         : null
    //     );
    // else
    set({
      trainGroupDto: {
        id: 0,
        title: "",
        description: "",
        duration: "",
        startOn: "",
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
        userId: "",
        // userId: TokenService.getUserId() ?? "",
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
        // trainGroupDateCancellationSubscribers: [],
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
          ...(state.trainGroupDto.trainGroupDates.find(
            (x) => x.id === newRow.trainGroupDateId
          )?.trainGroupParticipants || []),
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
    set((state) => {
      const traingroup = {
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
      };
      return {
        trainGroupDto: traingroup,
        selectedTrainGroupDate: traingroup.trainGroupDates.find(
          (x) => x.id === row.trainGroupDateId
        ),
      };
    }),

  deleteTrainGroupDateParticipant: (row: TrainGroupParticipantDto) =>
    set((state) => {
      const traingroup = {
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
            ],
          },
        ],
      };
      return {
        trainGroupDto: traingroup,
        selectedTrainGroupDate: traingroup.trainGroupDates.find(
          (x) => x.id === row.trainGroupDateId
        ),
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
