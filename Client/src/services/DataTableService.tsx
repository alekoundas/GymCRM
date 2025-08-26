import {
  DataTableFilterEvent,
  DataTablePageEvent,
  DataTableRowEditCompleteEvent,
  DataTableSortEvent,
  DataTableSortMeta,
} from "primereact/datatable";
import ApiService from "./ApiService";
import { ColumnEvent } from "primereact/column";
import { FormMode } from "../enum/FormMode";
import { DataTableDto } from "../model/datatable/DataTableDto";
import { DataTableSortDto } from "../model/datatable/DataTableSortDto";

export default class DataTableService<TEntity> {
  private controller: string;
  private dataTableDto: DataTableDto<TEntity>;
  private setLoading: any;
  private defaultUrlSearchQuery: string | null = null;
  private formMode: FormMode;
  private afterDataLoaded?: (
    data: DataTableDto<TEntity> | null
  ) => DataTableDto<TEntity> | null;

  public constructor(
    controller: string,
    dataTableDto: DataTableDto<TEntity>,
    setLoading: any,
    defaultUrlSearchQuery: string | null,
    formMode: FormMode,
    afterDataLoaded?: (
      data: DataTableDto<TEntity> | null
    ) => DataTableDto<TEntity> | null
  ) {
    this.controller = controller;
    this.dataTableDto = dataTableDto;
    this.setLoading = setLoading;
    this.defaultUrlSearchQuery = defaultUrlSearchQuery;
    this.formMode = formMode;
    this.afterDataLoaded = afterDataLoaded;
  }

  public loadData = async (
    urlQuery: string | null
  ): Promise<DataTableDto<TEntity> | null> => {
    if (urlQuery) {
      try {
        // Decode datatable filters and ordering from base 64
        const dtaTableQuery: DataTableDto<TEntity> = JSON.parse(atob(urlQuery));
        this.dataTableDto = dtaTableQuery;
      } catch (e: any) {
        console.log(e.message);
      }
    }
    return this.refreshData();
  };

  // public loadData = (urlQuery: string | null) => {
  //   if (urlQuery)
  //     try {
  //       // Decode datatable filters and ordering from base 64
  //       const dtaTableQuery: DataTableDto<TEntity> = JSON.parse(atob(urlQuery));
  //       this.dataTableDto = dtaTableQuery;
  //       this.setDataTableDto({ ...dtaTableQuery });
  //     } catch (e: any) {
  //       console.log(e.message);
  //     }
  //   this.refreshData();
  // };

  public onSort = (event: DataTableSortEvent) => {
    this.dataTableDto.sorts = [];
    if (event.multiSortMeta) {
      this.dataTableDto.dataTableSorts = event.multiSortMeta;
      event.multiSortMeta.forEach((value: DataTableSortMeta, index: number) =>
        this.dataTableDto.sorts?.push({
          fieldName: value.field,
          order: value.order,
        })
      );
    }
    this.refreshData();
  };

  public onFilter = (event: DataTableFilterEvent) => {
    this.dataTableDto.filters = [];
    this.dataTableDto.dataTableFilters = event.filters;

    // This doesnt work  [...event.filters]
    Object.entries(event.filters).map(([value, index]) =>
      this.dataTableDto.filters?.push({
        fieldName: value,
        filterType: event.filters[value]["matchMode"],
        value: event.filters[value]["value"],
      })
    );

    this.refreshData();
  };

  public onPage = (event: DataTablePageEvent) => {
    if (event.page) this.dataTableDto.page = event.page;
    if (event.rows) this.dataTableDto.rows = event.rows;
    if (event.pageCount) this.dataTableDto.pageCount = event.pageCount;

    this.refreshData();
  };

  public onCellEditComplete = (e: ColumnEvent) => {
    // let { rowData, newValue, field } = e;
    let { rowData, newValue, field, originalEvent: event } = e;
    rowData[field] = newValue;
    event.preventDefault();
  };

  public onRowEditComplete = (e: DataTableRowEditCompleteEvent) => {
    let { newData, field, index } = e;

    this.dataTableDto.data[index] = newData as TEntity;

    this.refreshData();
  };

  public refreshData = async (): Promise<DataTableDto<TEntity> | null> => {
    if (this.formMode === FormMode.ADD) {
      if (this.afterDataLoaded) {
        this.afterDataLoaded(this.dataTableDto);
      }

      return this.dataTableDto;
    } else {
      this.setLoading(true);
      return await ApiService.getDataGrid<TEntity>(
        this.controller,
        this.dataTableDto
      )
        .then(this.afterDataLoaded)
        .then((response) => {
          if (this.defaultUrlSearchQuery) {
            this.setUrlSearchQuery(response);
          }

          this.setLoading(false);
          return response;
        });
    }
  };

  private setUrlSearchQuery = (response: DataTableDto<TEntity> | null) => {
    if (response) {
      const queryData = { ...response, data: [] };

      // Encode datatable filters and ordering to base 64
      const searchQuery = btoa(JSON.stringify(queryData));
      if (searchQuery !== this.defaultUrlSearchQuery) {
        window.history.replaceState(
          null,
          "New Page Title",
          `/${this.controller}?search=${searchQuery}`
        );
      }
    }
  };

  // private setUrlSearchQuery = (response: DataTableDto<TEntity> | null) => {
  //   if (response) {
  //     response.data = [];

  //     // Encode datatable filters and ordering to base 64
  //     var searchQuery = btoa(JSON.stringify(response));

  //     if (searchQuery !== this.defaultUrlSearchQuery)
  //       window.history.replaceState(
  //         null,
  //         "New Page Title",
  //         `/${this.controller}?search=${searchQuery}`
  //       );
  //   }
  // };
}
function value(
  value: DataTableSortMeta,
  index: number,
  array: DataTableSortMeta[]
): void {
  throw new Error("Function not implemented.");
}
