import { Column, ColumnBodyOptions } from "primereact/column";
import {
  DataTable,
  DataTableRowEditCompleteEvent,
  DataTableValue,
} from "primereact/datatable";
import { DataTableDto } from "../../model/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import React, { useEffect, useState } from "react";
import DataTableService from "../../services/DataTableService";
import { Button } from "primereact/button";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { FormMode } from "../../enum/FormMode";

interface IField<TEntity> {
  controller: string;
  dataTable: DataTableDto<TEntity>;
  dataTableColumns: DataTableColumns[];
  formMode: FormMode;
  enableGridRowActions?: boolean;
  enableAddAction?: boolean;
  editMode?: DataTableEditModeEnum;
  filterDisplay?: DataTableFilterDisplayEnum;
  onRowEditInit?: (e: any) => void;
  onRowEditComplete?: (e: DataTableRowEditCompleteEvent) => void;
  onRowEditCancel?: (e: any) => void;
  onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
  // onAfterDataLoaded?: (
  //   data: DataTableDto<TEntity> | null
  // ) => DataTableDto<TEntity> | null;
  // triggerRefreshData?: React.MutableRefObject<(() => void) | undefined>;
  // triggerRequestData?: (data: () => TEntity[]) => void;
}

export default function DataTableComponent<TEntity extends DataTableValue>({
  controller,
  dataTable,
  dataTableColumns,
  formMode,
  enableGridRowActions = false,
  enableAddAction = false,
  editMode,
  filterDisplay,
  onRowEditInit,
  onRowEditComplete,
  onRowEditCancel,
  onButtonClick,
}: // onAfterDataLoaded,
// triggerRefreshData,
// triggerRequestData,
IField<TEntity>) {
  const [loading, setLoading] = useState(true);
  const [dataTableDto, setDataTableDto] =
    useState<DataTableDto<TEntity>>(dataTable);

  // const dataTableService = new DataTableService(
  //   controller,
  //   dataTable,
  //   setLoading,
  //   null,
  //   formMode,
  //   onAfterDataLoaded
  // );
  const onAfterDataLoaded = (
    data: DataTableDto<TEntity> | null
  ): DataTableDto<TEntity> | null => {
    if (data) {
      setDataTableDto(data);
    }
    return data;
  };

  const dataTableService = new DataTableService(
    controller,
    dataTable,
    setLoading,
    null,
    formMode,
    onAfterDataLoaded
  );

  // React.useEffect(() => {
  //   if (triggerRefreshData)
  //     triggerRefreshData.current = dataTableService.refreshData;
  // }, [triggerRefreshData]);

  // Sync dataTableDto with dataTable prop when it changes
  React.useEffect(() => {
    setDataTableDto(dataTable);
  }, [dataTable]);

  React.useEffect(() => {
    if (formMode === FormMode.ADD) {
      setLoading(false);
    } else {
      dataTableService.loadData(null);
    }
  }, []);

  // const getDataTableColumns = () => {
  //   let canAddAction =
  //     dataTableColumns.filter((x) => x.header === "Actions").length === 0;

  //   canAddAction = canAddAction && enableGridRowActions;

  //   if (canAddAction)
  //     dataTableColumns.push({
  //       field: "",
  //       header: "Actions",
  //       sortable: false,
  //       filter: false,
  //       filterPlaceholder: "",
  //       style: { width: "20%" },
  //       body: gridRowActions,
  //     });
  //   return dataTableColumns;
  // };

  const getDataTableColumns = () => {
    const columns = [...dataTableColumns];

    // In case Actions column already exists dont add it
    if (columns.every((x) => x.header !== "Actions")) {
      // In case editMode is set, add Edit inline button
      if (editMode)
        columns.push({
          field: "",
          header: "Actions",
          sortable: false,
          filter: false,
          filterPlaceholder: "",
          style: { width: "10%" },
          body: null,
          rowEditor: true, // enables row edit
        });
      // In case editMode is NOT set, add View,Edit,Delete buttons
      else if (enableGridRowActions)
        columns.push({
          field: "",
          header: "Actions",
          sortable: false,
          filter: false,
          filterPlaceholder: "",
          style: { width: "20%" },
          body: gridRowActions,
        });
    }
    return columns;
  };

  const gridRowActions = (rowData: TEntity, _options: ColumnBodyOptions) => (
    <React.Fragment>
      <Button
        icon="pi pi-eye"
        rounded
        outlined
        className="mr-2"
        severity="secondary"
        onClick={() => onButtonClick(ButtonTypeEnum.VIEW, rowData)}
      />
      <Button
        icon="pi pi-pencil"
        rounded
        outlined
        className="mr-2"
        onClick={() => onButtonClick(ButtonTypeEnum.EDIT, rowData)}
      />
      <Button
        icon="pi pi-trash"
        rounded
        outlined
        severity="danger"
        onClick={() => onButtonClick(ButtonTypeEnum.DELETE, rowData)}
      />
    </React.Fragment>
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
          onClick={() => {
            onButtonClick(ButtonTypeEnum.ADD);
            dataTableService.refreshData();
          }}
        />
      </div>
    );
  };

  return (
    <>
      <DataTable
        className="w-full"
        value={dataTableDto.data}
        lazy
        stripedRows
        emptyMessage="No data found."
        tableStyle={{ minWidth: "50rem" }}
        selectionMode="single"
        loading={loading}
        // Pagging.
        paginator
        rows={dataTable.rows}
        totalRecords={dataTable.pageCount}
        onPage={dataTableService.onPage}
        rowsPerPageOptions={[10, 25, 50, 100]}
        // paginatorLeft={paginatorLeft}
        paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
        currentPageReportTemplate={
          "1 to " + dataTable.rows + " out of " + dataTable.pageCount
        }
        // Filter.
        filterDisplay={filterDisplay}
        filters={dataTable.filters}
        onFilter={dataTableService.onFilter}
        // Sort.
        removableSort
        sortMode="multiple"
        onSort={dataTableService.onSort}
        multiSortMeta={dataTable.multiSortMeta}
        header={enableAddAction ? renderHeader() : null}
        // Edit row/column.
        editMode={editMode}
        onRowEditInit={onRowEditInit}
        onRowEditCancel={onRowEditCancel}
        onRowEditComplete={
          onRowEditComplete
            ? onRowEditComplete
            : dataTableService.onRowEditComplete
        }
      >
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
            rowEditor={col.rowEditor}
          />
        ))}
      </DataTable>
    </>
  );
}
