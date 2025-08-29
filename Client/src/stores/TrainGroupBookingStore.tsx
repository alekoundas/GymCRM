import { create } from "zustand";
import { TimeSlotRequestDto } from "../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../model/TimeSlotResponseDto";
import { TimeSlotRecurrenceDateDto } from "../model/TimeSlotRecurrenceDateDto";

interface useTrainGroupBookingStoreState {
  timeSlotRequestDto: TimeSlotRequestDto;
  timeSlotResponseDto: TimeSlotResponseDto[];
  timeSlotRecurrenceDateDto: TimeSlotRecurrenceDateDto[];
  selectedTimeSlotResponseDto: TimeSlotResponseDto | undefined;

  setTimeSlotRequestDto: (data: TimeSlotRequestDto) => void;
  setTimeSlotResponseDto: (data: TimeSlotResponseDto[]) => void;
  setSelectedTimeSlotResponseDto: (data: TimeSlotResponseDto) => void;

  updateTimeSlotResponseDto: (updates: Partial<TimeSlotResponseDto>) => void;
  addTimeSlotRecurrenceDateDto: (newRow: TimeSlotRecurrenceDateDto) => void;
  resetTimeSlotResponseDto: (id?: TimeSlotResponseDto) => void;
}

export const useTrainGroupBookingStore = create<useTrainGroupBookingStoreState>(
  (set) => ({
    timeSlotRequestDto: { selectedDate: new Date() },
    timeSlotResponseDto: [],
    timeSlotRecurrenceDateDto: [],
    selectedTimeSlotResponseDto: undefined,

    setTimeSlotRequestDto: (data) => set({ timeSlotRequestDto: data }),
    setTimeSlotResponseDto: (data) => set({ timeSlotResponseDto: data }),
    setSelectedTimeSlotResponseDto: (data: TimeSlotResponseDto) => {
      set({ selectedTimeSlotResponseDto: data });
    },

    updateTimeSlotResponseDto: (updates) =>
      set((state) => ({
        timeSlotResponseDto: { ...state.timeSlotResponseDto, ...updates },
      })),
    addTimeSlotRecurrenceDateDto: (newRow) =>
      set((state) => ({
        timeSlotRecurrenceDateDto: [...state.timeSlotRecurrenceDateDto, newRow],
      })),
    resetTimeSlotResponseDto: (id?: TimeSlotResponseDto) => {
      set({
        timeSlotResponseDto: [],
        // {
        //   title: "",
        //   description: "",
        //   trainerId: "",
        //   trainGroupId: -1,
        //   trainGroupDateId: -1,
        //   duration: new Date(),
        //   startOn: new Date(),
        //   spotsLeft: 0,
        //   recurrenceDates: [],
        // },
      });
    },
    // resetTrainGroupDto: async (id?: number) => {
    //   if (id)
    //     return await ApiService.get<TrainGroupDto>("TrainGroups", id).then(
    //       (value) => (value ? set({ trainGroupDto: value }) : null)
    //     );
    //   else
    //     set({
    //       timeSlotResponseDto: {
    //         title: "",
    //         description: "",
    //         trainerId: "",
    //         trainGroupId: -1,
    //         trainGroupDateId: -1,
    //         duration: new Date(),
    //         startOn: new Date(),
    //         spotsLeft: 0,
    //         recurrenceDates: [],
    //       },
    //     });
    // },
  })
);
