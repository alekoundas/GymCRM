import { create } from "zustand";
import { PhoneNumberDto } from "../model/entities/phone-number/PhoneNumberDto";
import { TokenService } from "../services/TokenService";

interface PhoneNumberStoreState {
  phoneNumberDto: PhoneNumberDto;
  setPhoneNumberDto: (data: PhoneNumberDto) => void;
  updatePhoneNumberDto: (updates: Partial<PhoneNumberDto>) => void;
  resetPhoneNumberDto: () => void;
}

export const usePhoneNumberStore = create<PhoneNumberStoreState>((set) => ({
  phoneNumberDto: new PhoneNumberDto(),
  setPhoneNumberDto: (data) => set({ phoneNumberDto: data }),

  updatePhoneNumberDto: (updates) =>
    set((state) => ({
      phoneNumberDto: { ...state.phoneNumberDto, ...updates },
    })),

  resetPhoneNumberDto: () => {
    set({
      phoneNumberDto: {
        ...new PhoneNumberDto(),
        userId: TokenService.getUserId() ?? "",
      },
    });
  },
}));
