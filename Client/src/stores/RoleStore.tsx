import { create } from "zustand";
import ApiService from "../services/ApiService";
import { RoleDto } from "../model/entities/role/RoleDto";

interface RoleStoreState {
  roleDto: RoleDto;
  setRoleDto: (data: RoleDto) => void;
  updateRoleDto: (updates: Partial<RoleDto>) => void;
  resetRoleDto: (id?: number) => Promise<any>;
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
  resetRoleDto: async (id?: number) => {
    if (id)
      return await ApiService.get<RoleDto>("roles", id).then((value) =>
        value ? set({ roleDto: value }) : null
      );
    else
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
