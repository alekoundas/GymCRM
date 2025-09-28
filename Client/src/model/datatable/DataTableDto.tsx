import { DataTableSortMeta } from "primereact/datatable";
import { DataTableSortDto } from "./DataTableSortDto";
import { DataTableFilterDto } from "./DataTableFilterDto";

export interface DataTableDto<TEntity> {
  first: number;
  rows: number;
  page: number;
  pageCount: number;
  totalRecords: number;

  data: TEntity[];
  sorts?: DataTableSortDto[];
  filters: DataTableFilterDto[];
  dataTableSorts: DataTableSortMeta[];
}

export class DataTableDto<TEntity> implements DataTableDto<TEntity> {
  first: number = 0;
  rows: number = 10;
  page: number = 0;
  pageCount: number = 0;
  totalRecords: number = 0;

  data: TEntity[] = [];
  sorts?: DataTableSortDto[] = [];
  filters: DataTableFilterDto[] = [];
  dataTableSorts: DataTableSortMeta[] = [];
}
