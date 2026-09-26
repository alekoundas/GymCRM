// An admin-defined label for grouping train groups - one tab each on the calendar.
export interface TrainGroupCategoryDto {
  id: number;
  name: string;
}

export class TrainGroupCategoryDto {
  id: number = 0;
  name: string = "";
}
