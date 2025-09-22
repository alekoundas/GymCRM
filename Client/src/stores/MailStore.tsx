import { create } from "zustand";
import { MailDto } from "../model/entities/mail/MailDto";
import { MailSendDto } from "../model/entities/mail/MailSendDto";

interface MailState {
  mailDto: MailDto;
  setMailDto: (data: MailDto) => void;
  updateMailDto: (updates: Partial<MailDto>) => void;
  resetMailDto: () => void;

  mailSendDto: MailSendDto;
  setMailSendDto: (data: MailDto) => void;
  updateMailSendDto: (updates: Partial<MailSendDto>) => void;
  resetMailSendDto: () => void;
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

  mailSendDto: new MailSendDto(),
  setMailSendDto: (data) => set({ mailSendDto: data }),
  updateMailSendDto: (updates) =>
    set((state) => ({
      mailSendDto: { ...state.mailSendDto, ...updates },
    })),
  resetMailSendDto: () => {
    set({
      mailSendDto: new MailSendDto(),
    });
  },
}));
