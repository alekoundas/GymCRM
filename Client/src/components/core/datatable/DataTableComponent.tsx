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
import DataTableService from "../../../services/DataTableService";
import { DataTableDto } from "../../../model/datatable/DataTableDto";
import { TokenService } from "../../../services/TokenService";
import { DataTableFilterDto } from "../../../model/datatable/DataTableFilterDto";
import DataTableGridRowActionsComponent from "./DataTableGridRowActionsComponent";

interface IField<TEntity> {
  controller: string;
  dataTableDto: DataTableDto<TEntity>;
  setDataTableDto: (dataTableDto: DataTableDto<TEntity>) => void;
  dataTableColumns: DataTableColumns<TEntity>[];
  formMode: FormMode;
  enableGridRowActions?: boolean;
  enableAddAction?: boolean;
  editMode?: DataTableEditModeEnum;
  filterDisplay?: DataTableFilterDisplayEnum;
  authorize?: boolean;
  loadDataOnInit?: boolean;
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
  enableGridRowActions = false,
  enableAddAction = false,
  editMode,
  filterDisplay,
  authorize = false,
  loadDataOnInit = true,
  onRowEditInit,
  onRowEditComplete,
  onRowEditCancel,
  onButtonClick,
  onAfterDataLoaded,
  triggerRefreshData,
  onSelect,
  selectedObject,
}: IField<TEntity>) {
  const [loading, setLoading] = useState(true);

  const afterDataLoaded = (
    data: DataTableDto<TEntity> | null
  ): DataTableDto<TEntity> | null => {
    // if parent has set the onAfterDataLoaded, call parent
    if (onAfterDataLoaded) {
      var dataResponse = onAfterDataLoaded(data);
      if (dataResponse) {
        // setDataTableDto(dataResponse);
        setDataTableDto({ ...dataTableDto, data: dataResponse.data });
      }
    }

    // Else do default action
    if (data) {
      setDataTableDto({ ...dataTableDto, data: data.data });
    }
    return data;
  };

  const dataTableService = new DataTableService(
    controller,
    setLoading,
    null,
    formMode,
    afterDataLoaded
  );

  // Initialize
  React.useEffect(() => {
    if (formMode === FormMode.ADD) {
      setLoading(false);
    }

    if (loadDataOnInit) {
      dataTableService.loadData(dataTableDto, null);
    } else {
      setLoading(false);
    }
  }, []);

  const refreshData = async (dto: DataTableDto<TEntity>) => {
    return await dataTableService.refreshData(dto);
  };

  React.useEffect(() => {
    if (triggerRefreshData) {
      triggerRefreshData.current = refreshData;
    }
  }, [triggerRefreshData]);

  // Sync dataTableDto with dataTable prop when it changes
  // React.useEffect(() => {
  //   setDataTableDto(dataTable);
  // }, [dataTable]);

  // React.useEffect(() => {
  //   if (dataTableDto?.filters && dataTableDto?.filters.length > 0) {
  //     // Log filters only if they have changed
  //     console.log("Filters updated:", JSON.stringify(dataTableDto.filters));
  //   }
  // }, [dataTableDto.filters]);

  React.useEffect(() => {
    // Log filters only if they have changed
    console.log("data updated:", JSON.stringify(dataTableDto.data));
  }, [dataTableDto.data]);

  const getDataTableColumns = () => {
    const columns = [...dataTableColumns];

    // In case Actions column already exists dont add it
    if (columns.filter((x) => x.header === "Actions")[0]) {
      return columns;
    }

    // In case editMode is set, add Edit inline button
    if (editMode === DataTableEditModeEnum.ROW)
      columns.push({
        field: "",
        header: "Actions",
        sortable: false,
        filter: false,
        filterPlaceholder: "",
        style: { width: "10%" },
        body: undefined,
        editor: true, // enables row edit
      });

    // In case editMode is NOT set, add View,Edit,Delete buttons
    if (enableGridRowActions)
      columns.push({
        field: "",
        header: "Actions",
        sortable: false,
        filter: false,
        filterPlaceholder: "",
        style: { width: "20%" },
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
    />
  );

  const renderHeader = () => {
    return (
      <div className="flex justify-content-between">
        <div></div>
        <Button
          type="button"
          icon="pi pi-plus"
          label="Add"
          outlined
          visible={
            authorize
              ? TokenService.isUserAllowed(controller + "_Add") &&
                enableAddAction
              : enableAddAction
          }
          onClick={() => {
            onButtonClick(ButtonTypeEnum.ADD);
          }}
        />
      </div>
    );
  };

  const mapFiltersToDataTableFilterMeta = (
    filters: DataTableFilterDto[] | undefined
  ): DataTableFilterMeta => {
    if (!filters || filters.length === 0) return {};
    const aaa = filters.reduce((acc, filter) => {
      return {
        ...acc,
        [filter.fieldName]: {
          value: filter.value,
          matchMode: filter.filterType || "equals",
        },
      };
    }, {} as DataTableFilterMeta);

    return aaa;
  };

  return (
    <>
      <DataTable
        className="w-full"
        value={dataTableDto.data}
        // key={"id"}
        lazy
        stripedRows
        emptyMessage="No data found."
        // Row selction.
        selectionMode="single"
        selection={selectedObject ?? undefined}
        onSelectionChange={onSelect}
        // Loading.
        loading={loading}
        // Pagging.
        paginator
        rows={dataTableDto.rows}
        totalRecords={dataTableDto.pageCount}
        onPage={(x) => dataTableService.onPage(dataTableDto, x)}
        rowsPerPageOptions={[10, 25, 50, 100]}
        // paginatorLeft={paginatorLeft}
        paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
        currentPageReportTemplate={
          "1 to " + dataTableDto.rows + " out of " + dataTableDto.pageCount
        }
        // Filter.
        filterDisplay={filterDisplay}
        filters={mapFiltersToDataTableFilterMeta(dataTableDto.filters)}
        onFilter={(x) => dataTableService.onFilter(dataTableDto, x)}
        // Sort.
        removableSort
        sortMode="multiple"
        onSort={(x) => dataTableService.onSort(dataTableDto, x)}
        multiSortMeta={dataTableDto.dataTableSorts}
        header={enableAddAction ? renderHeader() : null}
        // Edit row/column.
        editMode={editMode}
        onRowEditInit={onRowEditInit}
        onRowEditCancel={onRowEditCancel}
        onRowEditComplete={(x) =>
          onRowEditComplete
            ? onRowEditComplete(x)
            : dataTableService.onRowEditComplete(dataTableDto, x)
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
            style={col.style}
            body={col.body}
            showFilterMenu={false}
            editor={col.cellEditor}
            onCellEditComplete={
              col.onCellEditComplete
                ? col.onCellEditComplete
                : dataTableService.onCellEditComplete
            }
            onCellEditInit={col.onCellEditInit}
            rowEditor={col.editor}
          />
        ))}
      </DataTable>
    </>
  );
}
