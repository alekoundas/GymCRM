import { useTranslation } from "react-i18next";
import type { TranslationTypes } from "../model/core/translation-types/TranslationTypes";

export const useTranslator = () => {
  const { t } = useTranslation<"translation">();

  return <K extends keyof TranslationTypes>(key: K) =>
    t(key as any) as TranslationTypes[K];
};
