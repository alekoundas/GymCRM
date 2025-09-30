import { create } from "zustand";
import { RoleDto } from "../model/entities/role/RoleDto";

interface RoleStoreState {
  roleDto: RoleDto;
  setRoleDto: (data: RoleDto) => void;
  updateRoleDto: (updates: Partial<RoleDto>) => void;
  resetRoleDto: () => void;
}

export const useRoleStore = create<RoleStoreState>((set) => ({
  roleDto: {
    id: "",
    name: "",
    normalizedName: "",
    concurrencyStamp: "",
    claims: [],
  },
  setRoleDto: (data) => set({ roleDto: data }),
  updateRoleDto: (updates) =>
    set((state) => ({
      roleDto: { ...state.roleDto, ...updates },
    })),
  resetRoleDto: () => {
    set({
      roleDto: {
        id: "",
        name: "",
        normalizedName: "",
        concurrencyStamp: "",
        claims: [],
      },
    });
  },
}));
