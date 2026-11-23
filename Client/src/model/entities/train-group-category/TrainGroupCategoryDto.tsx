// An admin-defined label for grouping train groups - one tab each on the calendar.
export interface TrainGroupCategoryDto {
  id: number;
  name: string;
  // "#rrggbb". Empty on categories made before colours existed; show those through
  // categoryColorOf, which picks a preset for them.
  color: string;
}

export class TrainGroupCategoryDto {
  id: number = 0;
  name: string = "";
  color: string = "";
}
