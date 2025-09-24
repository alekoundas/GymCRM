export interface DataTableFilterDto {
  fieldName: string;
  value?: string | undefined;
  values?: string[] | undefined;
  filterType:
    | "startsWith"
    | "contains"
    | "notContains"
    | "endsWith"
    | "equals"
    | "notEquals"
    | "in"
    | "notIn"
    | "lt"
    | "lte"
    | "gt"
    | "gte"
    | "between"
    | "dateIs"
    | "dateIsNot"
    | "dateBefore"
    | "dateAfter"
    | "custom"
    | undefined;
}

export class DataTableFilterDto {
  fieldName: string = "";
  value?: string | undefined;
  values?: string[] | undefined;
  filterType:
    | "startsWith"
    | "contains"
    | "notContains"
    | "endsWith"
    | "equals"
    | "notEquals"
    | "in"
    | "notIn"
    | "lt"
    | "lte"
    | "gt"
    | "gte"
    | "between"
    | "dateIs"
    | "dateIsNot"
    | "dateBefore"
    | "dateAfter"
    | "custom"
    | undefined;
}
