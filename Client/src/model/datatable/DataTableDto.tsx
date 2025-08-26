import { DataTableFilterMeta, DataTableSortMeta } from "primereact/datatable";
import { DataTableSortDto } from "./DataTableSortDto";
import { DataTableFilterDto } from "./DataTableFilterDto";

export class DataTableDto<TEntity> {
  data: TEntity[] = [];
  first: number = 0;
  rows: number = 0;
  page: number = 0;
  pageCount: number = 0;
  sorts?: DataTableSortDto[] = [];
  filters?: DataTableFilterDto[] = [];
  dataTableSorts: DataTableSortMeta[] = [];
  dataTableFilters: DataTableFilterMeta = {};

  // getDataTableSort?: () => DataTableSortMeta[] = () => {
  //   return this.sorts
  //     .filter(
  //       (sort) => sort.fieldName && sort.order !== 0 && sort.order != null
  //     ) // Skip invalid or non-sorting entries
  //     .map(
  //       (sort) =>
  //         ({
  //           field: sort.fieldName,
  //           order: sort.order,
  //         } as DataTableSortMeta)
  //     );
  // };

  // getDataTableFilter?: () => DataTableFilterMeta = () => {
  //   return this.filters.reduce((filterMeta, filter) => {
  //     // Skip filters with undefined/null fieldName or value
  //     if (!filter.fieldName || filter.value == null) {
  //       return filterMeta;
  //     }

  //     // Map filterType to PrimeReact's FilterMatchMode (adjust as needed)
  //     const matchMode = filter.filterType ?? "equals"; // Default to "equals" if undefined

  //     // Add filter to the DataTableFilterMeta object
  //     filterMeta[filter.fieldName] = {
  //       value: filter.value,
  //       matchMode: matchMode,
  //     };

  //     return filterMeta;
  //   }, {} as DataTableFilterMeta);
  // };
}
