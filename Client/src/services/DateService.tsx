import { useCallback } from "react";
import { DayOfWeekEnum } from "../enum/DayOfWeekEnum";
import { useTranslator } from "./TranslatorService";

export const useDateService = () => {
  const { t } = useTranslator();

  const getUTCDate = (dateString: string | undefined): Date => {
    if (!dateString) return new Date(Date.UTC(2000, 0, 1, 0, 0, 0));

    const date = new Date(dateString);
    if (dateString.endsWith("Z")) {
      return new Date(
        date.getUTCFullYear(),
        date.getUTCMonth(),
        date.getUTCDay(),
        date.getUTCHours(),
        date.getUTCMinutes(),
        0
      );
    } else {
      return new Date(
        date.getFullYear(),
        date.getMonth(),
        date.getDay(),
        date.getHours(),
        date.getMinutes(),
        0
      );
    }
  };

  const getUTCTime = (dateString: string | undefined): Date => {
    if (!dateString) return new Date(Date.UTC(2000, 0, 1, 0, 0, 0));

    const date = new Date(dateString);
    if (dateString.endsWith("Z")) {
      return new Date(2000, 0, 1, date.getUTCHours(), date.getUTCMinutes(), 0);
    } else {
      return new Date(2000, 0, 1, date.getHours(), date.getMinutes(), 0);
    }
  };

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

  const getNextDayOfWeekDateUTC = useCallback(
    (dayOfWeek: DayOfWeekEnum, inputDate: Date): Date => {
      const dayNumberMap: { [key in DayOfWeekEnum]: number } = {
        [DayOfWeekEnum.SUNDAY]: 0,
        [DayOfWeekEnum.MONDAY]: 1,
        [DayOfWeekEnum.TUESDAY]: 2,
        [DayOfWeekEnum.WEDNESDAY]: 3,
        [DayOfWeekEnum.THURSDAY]: 4,
        [DayOfWeekEnum.FRIDAY]: 5,
        [DayOfWeekEnum.SATURDAY]: 6,
      };

      const dayNumberMapTranslated: { [key in string]: number } = {
        [t("SUNDAY").toLocaleUpperCase()]: 0,
        [t("MONDAY").toLocaleUpperCase()]: 1,
        [t("TUESDAY").toLocaleUpperCase()]: 2,
        [t("WEDNESDAY").toLocaleUpperCase()]: 3,
        [t("THURSDAY").toLocaleUpperCase()]: 4,
        [t("FRIDAY").toLocaleUpperCase()]: 5,
        [t("SATURDAY").toLocaleUpperCase()]: 6,
      };

      let targetDay = dayNumberMap[dayOfWeek];
      if (targetDay === undefined)
        targetDay = dayNumberMapTranslated[dayOfWeek];

      const inputDay = inputDate.getUTCDay(); // <-- UTC

      let daysToAdd = (targetDay - inputDay + 7) % 7;
      const resultDate = new Date(
        inputDate.getTime() + daysToAdd * 24 * 60 * 60 * 1000
      );

      resultDate.setUTCHours(0, 0, 0, 0); // <-- Normalize to UTC midnight

      return resultDate;
    },
    []
  );

  const getNextDayOfMonthDateUTC = useCallback(
    (dayOfMonth: number, inputDate: Date): Date => {
      if (dayOfMonth < 1 || dayOfMonth > 31) {
        throw new Error("dayOfMonth must be between 1 and 31");
      }

      const inputDay = inputDate.getUTCDate(); // <-- UTC
      let resultYear = inputDate.getUTCFullYear(); // <-- UTC
      let resultMonth = inputDate.getUTCMonth(); // <-- UTC

      if (inputDay <= dayOfMonth) {
        // Same month
      } else {
        // Next month
        resultMonth += 1;
        if (resultMonth > 11) {
          resultMonth = 0;
          resultYear += 1;
        }
      }

      // Use Date.UTC for precise UTC midnight
      const resultTimestamp = Date.UTC(
        resultYear,
        resultMonth,
        dayOfMonth,
        0,
        0,
        0,
        0
      );
      const resultDate = new Date(resultTimestamp);

      return resultDate;
    },
    []
  );

  return {
    getUTCDate,
    getUTCTime,
    getDayOfWeekFromDate,
    translateEnum,
    getNextDayOfWeekDateUTC,
    getNextDayOfMonthDateUTC,
  };
};
