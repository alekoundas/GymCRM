import { create } from "zustand";
import { UserDto } from "../model/entities/user/UserDto";
import { UserPasswordChangeDto } from "../model/entities/user/UserPasswordChangeDto";

interface UserStoreState {
  userDto: UserDto;
  setUserDto: (data: UserDto) => void;
  updateUserDto: (updates: Partial<UserDto>) => void;
  resetUserDto: () => void;

  userPasswordChangeDto: UserPasswordChangeDto;
  setUserPasswordChangeDto: (data: UserPasswordChangeDto) => void;
  resetUserPasswordChangeDto: () => void;
}

export const useUserStore = create<UserStoreState>((set) => ({
  userDto: new UserDto(),
  setUserDto: (data) => set({ userDto: data }),

  updateUserDto: (updates) =>
    set((state) => ({
      userDto: { ...state.userDto, ...updates },
    })),

  resetUserDto: () => {
    set({
      userDto: new UserDto(),
    });
  },

  userPasswordChangeDto: new UserPasswordChangeDto(),
  setUserPasswordChangeDto: (data) => set({ userPasswordChangeDto: data }),
  resetUserPasswordChangeDto: () => {
    set({
      userPasswordChangeDto: new UserPasswordChangeDto(),
    });
  },
}));
