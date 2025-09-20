import { create } from "zustand";
import { MailDto } from "../model/entities/email-history/EmailHistoryDto";

interface MailState {
  mailDto: MailDto;
  setMailDto: (data: MailDto) => void;
  updateMailDto: (updates: Partial<MailDto>) => void;
  resetMailDto: () => void;
}

export const useMailStore = create<MailState>((set) => ({
  mailDto: new MailDto(),
  setMailDto: (data) => set({ mailDto: data }),
  updateMailDto: (updates) =>
    set((state) => ({
      mailDto: { ...state.mailDto, ...updates },
    })),
  resetMailDto: () => {
    set({
      mailDto: new MailDto(),
    });
  },
}));
