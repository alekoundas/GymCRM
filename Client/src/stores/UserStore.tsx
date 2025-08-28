import { create } from "zustand";
import { UserDto } from "../model/entities/user/UserDto";

interface UserStoreState {
  userDto: UserDto;
  setUserDto: (data: UserDto) => void;
  updateUserDto: (updates: Partial<UserDto>) => void;
  resetUserDto: () => void;
}

export const useUserStore = create<UserStoreState>((set) => ({
  userDto: {
    id: "",
    userName: "",
    email: "",
    roleId: "",
  },
  setUserDto: (data) => set({ userDto: data }),

  updateUserDto: (updates) =>
    set((state) => ({
      userDto: { ...state.userDto, ...updates },
    })),

  resetUserDto: () => {
    set({
      userDto: {
        id: "",
        userName: "",
        email: "",
        roleId: "",
      },
    });
  },
}));
