import i18n, { TFunction } from "i18next";
import { initReactI18next } from "react-i18next";
import Backend from "i18next-http-backend";
import { LocalStorageService } from "../services/LocalStorageService";

i18n
  .use(Backend)
  .use(initReactI18next)
  .init({
    // resources: {} as TranslationTypes,
    fallbackLng: "en",
    lng: LocalStorageService.getLanguage() || "en",
    interpolation: { escapeValue: false }, // React handles escaping
    backend: { loadPath: "/translations/{{lng}}.json" },
    detection: {
      order: ["localStorage", "navigator"], // Prioritize local storage
      caches: ["localStorage"], // Persist choice
    },
  });

export default i18n;
// export type TypedTFunction = TFunction<"translation", undefined>;
