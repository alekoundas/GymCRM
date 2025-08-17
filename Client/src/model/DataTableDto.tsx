import { DataTableFilterMeta, DataTableSortMeta } from "primereact/datatable";

export class DataTableDto<TEntity> {
  data: TEntity[] = [];
  first: number = 0;
  rows: number = 0;
  page: number = 0;
  pageCount: number = 0;
  multiSortMeta: DataTableSortMeta[] = [];
  filters: DataTableFilterMeta = {};
}
