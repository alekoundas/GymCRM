export interface DataTableSortDto {
  field: string; //eg. title
  fieldDb: string; //eg. Title
  order: 1 | 0 | -1 | null | undefined;
}

export class DataTableSortDto {
  field: string; //eg. title
  fieldDb: string; //eg. Title
  order: 1 | 0 | -1 | null | undefined;
}
