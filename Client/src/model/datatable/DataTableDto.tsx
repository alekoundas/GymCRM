import { DataTableFilterMeta, DataTableSortMeta } from "primereact/datatable";
import { DataTableSortDto } from "./DataTableSortDto";
import { DataTableFilterDto } from "./DataTableFilterDto";

export interface DataTableDto<TEntity> {
  data: TEntity[];
  first: number;
  rows: number;
  page: number;
  pageCount: number;
  sorts?: DataTableSortDto[];
  filters: DataTableFilterDto[];
  dataTableSorts: DataTableSortMeta[];
  // dataTableFilters?: DataTableFilterMeta;
}

export class DataTableDto<TEntity> {
  data: TEntity[] = [];
  first: number = 0;
  rows: number = 0;
  page: number = 0;
  pageCount: number = 0;
  sorts?: DataTableSortDto[] = [];
  filters: DataTableFilterDto[] = [];
  dataTableSorts: DataTableSortMeta[] = [];
  // dataTableFilters?: DataTableFilterMeta = this.filters.reduce(
  //   (accumulator, currentValue) => {
  //     if (currentValue.fieldName && currentValue.filterType) {
  //       console.log(currentValue.filterType);
  //       accumulator[currentValue.fieldName] = {
  //         value: currentValue.value,
  //         matchMode: currentValue.filterType,
  //       };
  //     }
  //     return accumulator;
  //   },
  //   {} as DataTableFilterMeta
  // );
}
