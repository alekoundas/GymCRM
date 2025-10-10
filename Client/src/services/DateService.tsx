import { useCallback } from "react";
import { DayOfWeekEnum } from "../enum/DayOfWeekEnum";
import { useTranslator } from "./TranslatorService";

export const useDateService = () => {
  const { t } = useTranslator();

  const getDayOfWeekFromDate = useCallback(
    (date: Date | undefined): string | undefined => {
      const dayOfWeekMap: { [key: number]: DayOfWeekEnum } = {
        0: DayOfWeekEnum.SUNDAY,
        1: DayOfWeekEnum.MONDAY,
        2: DayOfWeekEnum.TUESDAY,
        3: DayOfWeekEnum.WEDNESDAY,
        4: DayOfWeekEnum.THURSDAY,
        5: DayOfWeekEnum.FRIDAY,
        6: DayOfWeekEnum.SATURDAY,
      };

      if (!date) return undefined;

      const dayOfWeek = dayOfWeekMap[new Date(date).getDay()];
      return translateEnum(dayOfWeek);
    },
    [t]
  );

  const translateEnum = useCallback(
    (dayOfWeek: DayOfWeekEnum | undefined): string => {
      switch (dayOfWeek?.toUpperCase()) {
        case DayOfWeekEnum.MONDAY:
          return t("MONDAY");
        case DayOfWeekEnum.TUESDAY:
          return t("TUESDAY");
        case DayOfWeekEnum.WEDNESDAY:
          return t("WEDNESDAY");
        case DayOfWeekEnum.THURSDAY:
          return t("THURSDAY");
        case DayOfWeekEnum.FRIDAY:
          return t("FRIDAY");
        case DayOfWeekEnum.SATURDAY:
          return t("SATURDAY");
        case DayOfWeekEnum.SUNDAY:
          return t("SUNDAY");
        default:
          return "";
      }
    },
    [t]
  );

  return {
    getDayOfWeekFromDate,
    translateEnum,
  };
};
