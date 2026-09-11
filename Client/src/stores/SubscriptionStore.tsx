import { create } from "zustand";
import { SubscriptionDto } from "../model/entities/subscription/SubscriptionDto";
import { SubscriptionAddDto } from "../model/entities/subscription/SubscriptionAddDto";
import { SubscriptionDecideDto } from "../model/entities/subscription/SubscriptionDecideDto";

interface SubscriptionStoreState {
  subscriptionDto: SubscriptionDto;
  subscriptionAddDto: SubscriptionAddDto;
  subscriptionDecideDto: SubscriptionDecideDto;

  setSubscriptionDto: (data: SubscriptionDto) => void;
  resetSubscriptionDto: () => void;

  setSubscriptionAddDto: (data: SubscriptionAddDto) => void;
  updateSubscriptionAddDto: (updates: Partial<SubscriptionAddDto>) => void;
  resetSubscriptionAddDto: () => void;

  setSubscriptionDecideDto: (data: SubscriptionDecideDto) => void;
  updateSubscriptionDecideDto: (updates: Partial<SubscriptionDecideDto>) => void;
  resetSubscriptionDecideDto: () => void;
}

export const useSubscriptionStore = create<SubscriptionStoreState>((set) => ({
  subscriptionDto: new SubscriptionDto(),
  subscriptionAddDto: new SubscriptionAddDto(),
  subscriptionDecideDto: new SubscriptionDecideDto(),

  setSubscriptionDto: (data) => set({ subscriptionDto: data }),
  resetSubscriptionDto: () => set({ subscriptionDto: new SubscriptionDto() }),

  setSubscriptionAddDto: (data) => set({ subscriptionAddDto: data }),
  updateSubscriptionAddDto: (updates) =>
    set((state) => ({
      subscriptionAddDto: { ...state.subscriptionAddDto, ...updates },
    })),
  resetSubscriptionAddDto: () =>
    set({ subscriptionAddDto: new SubscriptionAddDto() }),

  setSubscriptionDecideDto: (data) => set({ subscriptionDecideDto: data }),
  updateSubscriptionDecideDto: (updates) =>
    set((state) => ({
      subscriptionDecideDto: { ...state.subscriptionDecideDto, ...updates },
    })),
  resetSubscriptionDecideDto: () =>
    set({ subscriptionDecideDto: new SubscriptionDecideDto() }),
}));
