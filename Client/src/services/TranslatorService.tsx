import { useTranslation } from "react-i18next";
import type { TranslationTypes } from "../model/core/translation-types/TranslationTypes";
import { LocalStorageService } from "./LocalStorageService";
import { locale } from "primereact/api";

export const useTranslator = () => {
  const { t, i18n } = useTranslation<"translation">();

  const setLanguage = (lng: string) => {
    i18n.changeLanguage(lng); // Switch json file for i18n
    LocalStorageService.setLanguage(lng); // Persist

    // i18n.on("loaded", () => {
    //   locale(lng); // Update PrimeReact
    // });
  };

  // Typed t function
  const typedT = <K extends keyof TranslationTypes>(key: K) =>
    t(key as any) as TranslationTypes[K];

  return { t: typedT, setLanguage, i18n };
};
