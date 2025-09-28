import {
  DataTableFilterEvent,
  DataTableFilterMetaData,
  DataTablePageEvent,
  DataTableRowEditCompleteEvent,
  DataTableSortEvent,
  DataTableSortMeta,
} from "primereact/datatable";
import ApiService from "./ApiService";
import { ColumnEvent } from "primereact/column";
import { FormMode } from "../enum/FormMode";
import { DataTableDto } from "../model/datatable/DataTableDto";
import { DataTableFilterDto } from "../model/datatable/DataTableFilterDto";

export default class DataTableService<TEntity> {
  private controller: string;
  // private dataTableDto: DataTableDto<TEntity>;
  private setLoading: any;
  private defaultUrlSearchQuery: string | null = null;
  private formMode: FormMode;
  private afterDataLoaded?: (
    data: DataTableDto<TEntity> | null
  ) => DataTableDto<TEntity> | null;

  public constructor(
    controller: string,
    // dataTableDto: DataTableDto<TEntity>,
    setLoading: any,
    defaultUrlSearchQuery: string | null,
    formMode: FormMode,
    afterDataLoaded?: (
      data: DataTableDto<TEntity> | null
    ) => DataTableDto<TEntity> | null
  ) {
    this.controller = controller;
    // this.dataTableDto = dataTableDto;
    this.setLoading = setLoading;
    this.defaultUrlSearchQuery = defaultUrlSearchQuery;
    this.formMode = formMode;
    this.afterDataLoaded = afterDataLoaded;
  }

  public loadData = async (
    dataTableDto: DataTableDto<TEntity>,
    urlQuery: string | null
  ): Promise<DataTableDto<TEntity> | null> => {
    // if (urlQuery) {
    //   try {
    // Decode datatable filters and ordering from base 64
    // const dtaTableQuery: DataTableDto<TEntity> = JSON.parse(atob(urlQuery));
    // dataTableDto = dtaTableQuery;
    //   } catch (e: any) {
    //     console.log(e.message);
    //   }
    // }
    return this.refreshData(dataTableDto);
  };

  public onSort = (
    dataTableDto: DataTableDto<TEntity>,
    event: DataTableSortEvent
  ) => {
    dataTableDto.sorts = [];
    if (event.multiSortMeta) {
      dataTableDto.dataTableSorts = event.multiSortMeta;
      event.multiSortMeta.forEach((value: DataTableSortMeta, index: number) =>
        dataTableDto.sorts?.push({
          fieldName: value.field,
          order: value.order,
        })
      );
    }
    this.refreshData(dataTableDto);
  };

  public onFilter = (
    dataTableDto: DataTableDto<TEntity>,
    event: DataTableFilterEvent
  ) => {
    // dataTableDto.dataTableFilters;
    dataTableDto.filters = [];

    Object.entries(event.filters).map(([value, index]) => {
      const filterField = event.filters[value] as DataTableFilterMetaData;

      const filter = {
        fieldName: value,
        filterType: filterField.matchMode,
      } as DataTableFilterDto;

      if (Array.isArray(filterField.value)) {
        filter.values = filterField.value ?? [];
      } else {
        filter.value = filterField.value;
      }

      dataTableDto.filters?.push(filter);
    });

    this.refreshData(dataTableDto);
  };

  public onPage = (
    dataTableDto: DataTableDto<TEntity>,
    event: DataTablePageEvent
  ) => {
    dataTableDto.page = event.page ?? 0;
    dataTableDto.rows = event.rows;
    dataTableDto.first = event.first;

    this.refreshData(dataTableDto);
  };

  public onCellEditComplete = (e: ColumnEvent) => {
    let { rowData, newValue, field, originalEvent: event } = e;
    rowData[field] = newValue;
    event.preventDefault();
  };

  public onRowEditComplete = (
    dataTableDto: DataTableDto<TEntity>,
    e: DataTableRowEditCompleteEvent
  ) => {
    let { newData, field, index } = e;

    dataTableDto.data[index] = newData as TEntity;
    this.refreshData(dataTableDto);
  };

  public refreshData = async (
    dataTableDto: DataTableDto<TEntity>
  ): Promise<DataTableDto<TEntity> | null> => {
    if (this.formMode === FormMode.ADD) {
      if (this.afterDataLoaded) {
        this.afterDataLoaded(dataTableDto);
      }

      return dataTableDto;
    } else {
      this.setLoading(true);
      dataTableDto.data = [];
      return await ApiService.getDataGrid<TEntity>(
        this.controller,
        dataTableDto
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
    // if (response) {
    //   const queryData = { ...response, data: [] };
    //   // Encode datatable filters and ordering to base 64
    //   const searchQuery = btoa(JSON.stringify(queryData));
    //   if (searchQuery !== this.defaultUrlSearchQuery) {
    //     window.history.replaceState(
    //       null,
    //       "New Page Title",
    //       `/${this.controller}?search=${searchQuery}`
    //     );
    //   }
    // }
  };
}
