import { DayOfWeekEnum } from "../enum/DayOfWeekEnum";

export interface TrainGroupDateDto {
  getDateFromDayOfWeek: (
    dayOfWeek: DayOfWeekEnum | undefined
  ) => Date | undefined;
  getDayOfWeekFromDate: (date: Date | undefined) => DayOfWeekEnum | undefined;
}

export class DateService {
  public static getDateFromDayOfWeek = (
    dayOfWeek: DayOfWeekEnum | undefined
  ): Date | undefined => {
    const daysOfWeek = {
      sunday: 0,
      monday: 1,
      tuesday: 2,
      wednesday: 3,
      thursday: 4,
      friday: 5,
      saturday: 6,
    };

    if (!dayOfWeek) return undefined;

    const dayOfWeekNum = daysOfWeek[dayOfWeek.toLowerCase()];

    // Create default date.
    const date = new Date(2000, 0, 1, 0, 0, 0, 0);

    // Get the day of the week.
    const firstDayOfMonth = date.getDay();

    // Calculate the offset to the first occurrence of the desired day
    let offset = dayOfWeekNum - firstDayOfMonth;
    if (offset < 0) {
      offset += 7; // If the target day is before the 1st, move to the next week
    }

    // Set the date to the first occurrence of the desired day
    date.setDate(1 + offset);

    return date;
  };

  public static getDayOfWeekFromDate = (
    date: Date | undefined
  ): DayOfWeekEnum | undefined => {
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

    return dayOfWeekMap[date.getDay()];
  };
}
