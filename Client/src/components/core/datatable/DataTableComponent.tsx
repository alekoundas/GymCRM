import { Button } from "primereact/button";
import { Column, ColumnBodyOptions } from "primereact/column";
import {
  DataTable,
  DataTableFilterMeta,
  DataTableRowEditCompleteEvent,
  DataTableSelectionSingleChangeEvent,
  DataTableValue,
  DataTableValueArray,
} from "primereact/datatable";
import React, { useState } from "react";
import { ButtonTypeEnum } from "../../../enum/ButtonTypeEnum";
import { DataTableEditModeEnum } from "../../../enum/DataTableEditModeEnum";
import { DataTableFilterDisplayEnum } from "../../../enum/DataTableFilterDisplayEnum";
import { FormMode } from "../../../enum/FormMode";
import { DataTableColumns } from "../../../model/datatable/DataTableColumns";

import { DataTableDto } from "../../../model/datatable/DataTableDto";
import { TokenService } from "../../../services/TokenService";
import DataTableGridRowActionsComponent from "./DataTableGridRowActionsComponent";
import { useDataTableService } from "../../../services/DataTableService";
import { useTranslator } from "../../../services/TranslatorService";
import { useSearchParams } from "react-router-dom";
import { DataTableSortDto } from "../../../model/datatable/DataTableSortDto";

interface IField<TEntity> {
  controller: string;
  dataTableDto: DataTableDto<TEntity>;
  setDataTableDto: (dataTableDto: DataTableDto<TEntity>) => void;
  dataTableColumns: DataTableColumns<TEntity>[];
  formMode: FormMode;
  editMode?: DataTableEditModeEnum;
  filterDisplay?: DataTableFilterDisplayEnum;
  authorize?: boolean;
  loadDataOnInit?: boolean;
  availableGridRowButtons?: ButtonTypeEnum[];
  onRowEditInit?: (e: any) => void;
  onRowEditComplete?: (e: DataTableRowEditCompleteEvent) => void;
  onRowEditCancel?: (e: any) => void;
  onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
  onAfterDataLoaded?: (
    data: DataTableDto<TEntity> | null
  ) => DataTableDto<TEntity> | null;
  triggerRefreshData?: React.MutableRefObject<
    ((dto: DataTableDto<TEntity>) => void) | undefined
  >;
  // Remembers filters, sorting and paging in the query string, so a filtered grid
  // survives a reload and can be sent to someone. Off by default: a grid inside a
  // dialog or a tab has no business touching the address bar.
  isUrlStateEnabled?: boolean;
  // Prefix that keeps two grids on the same page out of each other's parameters.
  urlStateKey?: string;
  // Filters the page forces itself, which have no place in a link - the member's
  // own userId, for instance.
  urlStateExcludedFields?: string[];

  onSelect?: (
    e: DataTableSelectionSingleChangeEvent<DataTableValueArray>
  ) => void;
  selectedObject?: TEntity | undefined;
}

export default function DataTableComponent<TEntity extends DataTableValue>({
  controller,
  dataTableDto,
  setDataTableDto,
  dataTableColumns,
  formMode,
  editMode,
  filterDisplay,
  authorize = false,
  loadDataOnInit = true,
  availableGridRowButtons = [],
  onRowEditInit,
  onRowEditComplete,
  onRowEditCancel,
  onButtonClick,
  onAfterDataLoaded,
  triggerRefreshData,
  isUrlStateEnabled = false,
  urlStateKey,
  urlStateExcludedFields = [],
  onSelect,
  selectedObject,
}: IField<TEntity>) {
  const { t } = useTranslator();
  const [loading, setLoading] = useState(true);
  const [, setSearchParams] = useSearchParams();

  //
  //          Grid state in the query string. Values only - the page already
  //          declares each filter's type, so the url never has to carry it.
  //

  // Filters whose value is a list rather than a single string.
  const isMultiValueFilter = (filterType: string | undefined): boolean =>
    filterType === "in" || filterType === "notIn" || filterType === "between";

  const urlName = (name: string): string =>
    urlStateKey ? `${urlStateKey}.${name}` : name;

  const isUrlField = (fieldName: string): boolean =>
    !urlStateExcludedFields.includes(fieldName);

  // Applied to the dto in place, before the first load, so a shared link costs one
  // request rather than loading everything and then filtering it. In place because
  // onFilter, onSort and onPage all mutate this same object and afterDataLoaded
  // spreads it - a copy here would be thrown away by the first load.
  const applyUrlState = (dto: DataTableDto<TEntity>) => {
    const params = new URLSearchParams(window.location.search);

    const filters = dto.filters.map((filter) => {
      if (!isUrlField(filter.fieldName)) return filter;

      const values = params.getAll(urlName(filter.fieldName));
      if (values.length === 0) return filter;

      return isMultiValueFilter(filter.filterType)
        ? { ...filter, values: values }
        : { ...filter, value: values[0] };
    });

    // "-createdOn,title" - a leading minus is descending.
    const sortParam = params.get(urlName("sort"));
    const sorts: DataTableSortDto[] = (sortParam?.split(",") ?? [])
      .filter((x) => x.length > 0)
      .map((x) => ({
        fieldName: x.startsWith("-") ? x.substring(1) : x,
        order: x.startsWith("-") ? -1 : 1,
      }));

    const rows = Number(params.get(urlName("rows"))) || dto.rows;
    // One-based in the url, because that is the number the paginator shows.
    const page = Math.max(0, (Number(params.get(urlName("page"))) || 1) - 1);

    dto.filters = filters;

    if (sorts.length > 0) {
      dto.sorts = sorts;
      dto.dataTableSorts = sorts.map((x) => ({
        field: x.fieldName,
        order: x.order,
      }));
    }

    dto.rows = rows;
    dto.page = page;
    dto.first = page * rows;
  };

  // Written after every load, when the dto has settled. Only this grid's own
  // parameters are touched, so anything else on the url is left alone.
  const writeUrlState = (dto: DataTableDto<TEntity>) => {
    setSearchParams(
      (previous) => {
        const params = new URLSearchParams(previous);

        dto.filters.forEach((filter) => {
          if (!isUrlField(filter.fieldName)) return;

          params.delete(urlName(filter.fieldName));

          const values = isMultiValueFilter(filter.filterType)
            ? filter.values ?? []
            : filter.value != null && filter.value !== ""
              ? [filter.value]
              : [];

          values.forEach((value) =>
            params.append(urlName(filter.fieldName), value),
          );
        });

        const sort = (dto.sorts ?? [])
          .map((x) => (x.order === -1 ? "-" : "") + x.fieldName)
          .join(",");

        params.delete(urlName("sort"));
        if (sort.length > 0) params.set(urlName("sort"), sort);

        params.delete(urlName("page"));
        if (dto.page > 0) params.set(urlName("page"), (dto.page + 1).toString());

        params.delete(urlName("rows"));
        if (dto.rows !== new DataTableDto<TEntity>().rows)
          params.set(urlName("rows"), dto.rows.toString());

        return params;
      },
      // Replace, or every keystroke in a filter box becomes a history entry and
      // the back button stops meaning anything.
      { replace: true },
    );
  };

  const afterDataLoaded = (
    data: DataTableDto<TEntity> | null
  ): DataTableDto<TEntity> | null => {
    let updateData: DataTableDto<TEntity> | null = data;

    // if parent has set the onAfterDataLoaded, call parent
    if (onAfterDataLoaded) {
      updateData = onAfterDataLoaded(data);
    }

    if (updateData) {
      const merged = {
        ...dataTableDto,
        data: updateData.data,
        totalRecords: updateData.totalRecords,
        pageCount: updateData.pageCount,
        page: updateData.page,
        first: updateData.first,
        rows: updateData.rows,
      };

      setDataTableDto(merged);
      if (isUrlStateEnabled) writeUrlState(merged);
    }

    return updateData;
  };

  // UPDATED: Use hook with memoized afterDataLoaded
  const {
    loadData,
    onSort,
    onFilter,
    onPage,
    onCellEditComplete,
    onRowEditComplete: serviceOnRowEditComplete,
    refreshData,
  } = useDataTableService<TEntity>({
    controller,
    setLoading,
    defaultUrlSearchQuery: null,
    formMode,
    afterDataLoaded,
  });

  // Initialize
  React.useEffect(() => {
    if (formMode === FormMode.ADD) {
      setLoading(false);
    }

    if (loadDataOnInit) {
      // Seeded first so the very first request already carries the url's filters.
      if (isUrlStateEnabled) applyUrlState(dataTableDto);

      refreshData(dataTableDto);
    } else {
      setLoading(false);
    }
  }, []);

  const isInitialMount = React.useRef(true);
  const refreshAllData = async (dto: DataTableDto<TEntity>) => {
    if (triggerRefreshData && !isInitialMount.current) {
      return await refreshData(dto);
    }
  };

  React.useEffect(() => {
    if (triggerRefreshData) {
      triggerRefreshData.current = refreshAllData;
    }
    isInitialMount.current = false;
  }, [triggerRefreshData]);

  // Log if they have changed
  // React.useEffect(() => {
  //   console.log("data updated:", JSON.stringify(dataTableDto.data));
  // }, [dataTableDto.data]);

  const getDataTableColumns = () => {
    const columns = dataTableColumns;

    // In case editMode is set, add Edit inline button
    if (editMode === DataTableEditModeEnum.ROW)
      columns.push({
        field: "",
        header: t("Actions"),
        sortable: false,
        filter: false,
        filterPlaceholder: "",
        style: { width: "10%" },
        body: undefined,
        editor: true, // enables row edit
      });

    // In case editMode is NOT set, add View,Edit,Delete buttons
    if (availableGridRowButtons.length > 0)
      columns.push({
        field: "",
        header: t("Actions"),
        sortable: false,
        filter: false,
        filterPlaceholder: "",
        style: { width: "10%" },
        body: gridRowActions,
      });
    return columns;
  };

  const gridRowActions = (rowData: TEntity, _options: ColumnBodyOptions) => (
    <DataTableGridRowActionsComponent
      rowData={rowData}
      onButtonClick={onButtonClick}
      authorize={authorize}
      controller={controller}
      availableGridRowButtons={availableGridRowButtons}
    />
  );

  const renderHeader = () => {
    dataTableDto.data;
    let isVisible: boolean = availableGridRowButtons.some(
      (x) => x === ButtonTypeEnum.ADD
    );

    if (authorize)
      isVisible = isVisible && TokenService.isUserAllowed(controller + "_Add");

    if (!isVisible) return;

    return (
      <div className="flex justify-content-between">
        <div></div>
        <Button
          type="button"
          icon="pi pi-plus"
          label={t("Add")}
          outlined
          visible={isVisible}
          onClick={() => {
            onButtonClick(ButtonTypeEnum.ADD);
          }}
        />
      </div>
    );
  };

  const dataTableFilters = () => {
    const dataTableFilters: DataTableFilterMeta = dataTableDto.filters.reduce(
      (accumulator, currentValue) => {
        if (currentValue.fieldName && currentValue.filterType) {
          accumulator[currentValue.fieldName] = {
            value: currentValue.value ?? currentValue.values,
            matchMode: currentValue.filterType,
          };
        }
        return accumulator;
      },
      {} as DataTableFilterMeta
    );
    return dataTableFilters;
  };

  return (
    <>
      <DataTable
        className="w-full"
        value={dataTableDto.data}
        size="small"
        // key={"id"}
        lazy={formMode !== FormMode.ADD}
        stripedRows
        emptyMessage={t("No data found.")}
        // Row selction.
        selectionMode="single"
        selection={selectedObject ?? undefined}
        onSelectionChange={onSelect}
        // Loading.
        loading={loading}
        // Pagging.
        paginator
        first={dataTableDto.first}
        rows={dataTableDto.rows}
        totalRecords={dataTableDto.totalRecords}
        onPage={(x) => onPage(dataTableDto, x)}
        rowsPerPageOptions={[5, 10, 25, 50, 100]}
        paginatorRight={
          <>
            {dataTableDto.first + 1} to{" "}
            {dataTableDto.rows * (dataTableDto.page + 1)} out of{" "}
            {dataTableDto.totalRecords}
          </>
        }
        paginatorLeft={<></>}
        // Filter.
        filterDisplay={filterDisplay}
        filters={dataTableFilters()}
        onFilter={(x) => onFilter(dataTableDto, x)}
        // Sort.
        removableSort
        sortMode="multiple"
        onSort={(x) => onSort(dataTableDto, x)}
        multiSortMeta={dataTableDto.dataTableSorts}
        header={renderHeader()}
        // Edit row/column.
        editMode={editMode}
        onRowEditInit={onRowEditInit}
        onRowEditCancel={onRowEditCancel}
        onRowEditComplete={(x) =>
          onRowEditComplete
            ? onRowEditComplete(x)
            : serviceOnRowEditComplete(dataTableDto, x)
        }
      >
        {/* {selectedObject && (
          <Column
            selectionMode="single"
            headerStyle={{ width: "3rem" }}
          ></Column>
        )} */}

        {getDataTableColumns().map((col, _i) => (
          <Column
            key={col.field}
            field={col.field}
            header={col.header}
            sortable={col.sortable}
            filter={col.filter}
            filterPlaceholder={col.filterPlaceholder}
            filterElement={col.filterTemplate}
            style={col.style}
            body={col.body}
            showFilterMenu={false}
            editor={col.cellEditor}
            onCellEditComplete={
              col.onCellEditComplete
                ? col.onCellEditComplete
                : onCellEditComplete
            }
            onCellEditInit={col.onCellEditInit}
            rowEditor={col.editor}
          />
        ))}
      </DataTable>
    </>
  );
}
