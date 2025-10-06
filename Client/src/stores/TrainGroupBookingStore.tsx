import { create } from "zustand";
import { TimeSlotRequestDto } from "../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../model/TimeSlotResponseDto";
import { TimeSlotRecurrenceDateDto } from "../model/TimeSlotRecurrenceDateDto";
import { TokenService } from "../services/TokenService";
// import ApiService from "../services/ApiService";
import { TrainGroupParticipantUpdateDto } from "../model/entities/train-group-participant/TrainGroupParticipantUpdateDto";

interface useTrainGroupBookingStoreState {
  timeSlotRequestDto: TimeSlotRequestDto;
  timeSlotResponseDto: TimeSlotResponseDto[];
  trainGroupDateParticipantUpdateDto: TrainGroupParticipantUpdateDto;

  selectedTimeSlot: TimeSlotResponseDto | undefined;
  timeSlotRecurrenceDateDto: TimeSlotRecurrenceDateDto[];

  setTimeSlotRequestDto: (data: TimeSlotRequestDto) => void;
  setTimeSlotResponseDto: (data: TimeSlotResponseDto[]) => void;

  setSelectedTimeSlot: (data: TimeSlotResponseDto) => void;
  resetSelectedTimeSlotResponseDto: () => void;

  updateTrainGroupDateParticipantUpdateDto: (
    updates: Partial<TrainGroupParticipantUpdateDto>
  ) => void;
  updateTimeSlotResponseDto: (updates: Partial<TimeSlotResponseDto>) => void;

  resetTrainGroupDateParticipantUpdateDto: (id?: number) => void;
  resetTimeSlotResponseDto: (id?: TimeSlotResponseDto) => void;
  resetTimeSlotRequestDto: (date?: string) => void;
}

export const useTrainGroupBookingStore = create<useTrainGroupBookingStoreState>(
  (set) => ({
    timeSlotRequestDto: {
      selectedDate: new Date().toISOString(),
      userId: TokenService.getUserId() ?? "",
    },
    timeSlotResponseDto: [],
    trainGroupDateParticipantUpdateDto: {
      userId: TokenService.getUserId(),
      trainGroupId: -1,
      selectedDate: "",
      trainGroupParticipantDtos: [],
    },
    selectedTimeSlot: undefined,

    timeSlotRecurrenceDateDto: [],

    setTimeSlotRequestDto: (data) => set({ timeSlotRequestDto: data }),
    setTimeSlotResponseDto: (data) => set({ timeSlotResponseDto: data }),
    setSelectedTimeSlot: (data: TimeSlotResponseDto) => {
      set({ selectedTimeSlot: data });
    },

    updateTrainGroupDateParticipantUpdateDto: (updates) =>
      set((state) => ({
        trainGroupDateParticipantUpdateDto: {
          ...state.trainGroupDateParticipantUpdateDto,
          ...updates,
        },
      })),
    updateTimeSlotResponseDto: (updates) =>
      set((state) => ({
        timeSlotResponseDto: { ...state.timeSlotResponseDto, ...updates },
      })),

    resetTimeSlotResponseDto: (id?: TimeSlotResponseDto) => {
      set({
        timeSlotResponseDto: [],
      });
    },
    resetTrainGroupDateParticipantUpdateDto: (id?: number) => {
      set({
        trainGroupDateParticipantUpdateDto: {
          userId: TokenService.getUserId(),
          trainGroupId: -1,
          selectedDate: "",
          trainGroupParticipantDtos: [],
        },
      });
    },
    resetSelectedTimeSlotResponseDto: () =>
      set({ selectedTimeSlot: undefined }),
    resetTimeSlotRequestDto: (date?: string) => {
      if (date) {
        set((state) => ({
          timeSlotRequestDto: {
            ...state.timeSlotRequestDto,
            selectedDate: date,
          },
        }));

        // ApiService
        //   .timeslots("TrainGroupDates/TimeSlots", {
        //     userId: TokenService.getUserId() ?? "",
        //     selectedDate: date,
        //   })
        //   .then((response) => {
        //     if (response) {
        //       set((state) => ({ timeSlotResponseDto: response }));
        //     }
        //   });
      } else {
        set({ selectedTimeSlot: undefined });
      }
    },
  })
);
