import { create } from "zustand";
import { UserStatusDto } from "../model/entities/user-status/UserStatusDto";

interface UserStatusStoreState {
  userStatusDto: UserStatusDto;
  setUserStatusDto: (data: UserStatusDto) => void;
  updateUserStatusDto: (updates: Partial<UserStatusDto>) => void;
  resetUserStatusDto: () => void;
}

export const useUserStatusStore = create<UserStatusStoreState>((set) => ({
  userStatusDto: new UserStatusDto(),
  setUserStatusDto: (data) => set({ userStatusDto: data }),

  updateUserStatusDto: (updates) =>
    set((state) => ({
      userStatusDto: { ...state.userStatusDto, ...updates },
    })),

  resetUserStatusDto: () => {
    set({
      userStatusDto: {
        ...new UserStatusDto(),
      },
    });
  },
}));
