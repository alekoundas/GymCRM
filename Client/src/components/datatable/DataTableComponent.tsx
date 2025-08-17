import { Column, ColumnBodyOptions } from "primereact/column";
import { DataTable, DataTableValue } from "primereact/datatable";
import { DataTableDto } from "../../model/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import React, { useState } from "react";
import DataTableService from "../../services/DataTableService";
import { Button } from "primereact/button";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";

interface IField<TEntity> {
  controller: string;
  dataTable: DataTableDto<TEntity>;
  dataTableColumns: DataTableColumns[];
  enableGridRowActions?: boolean;
  enableAddAction?: boolean;
  editMode?: DataTableEditModeEnum;
  filterDisplay?: DataTableFilterDisplayEnum;
  onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
  onAfterDataLoaded?: (
    data: DataTableDto<TEntity> | null
  ) => DataTableDto<TEntity> | null;
  triggerRefreshData?: React.MutableRefObject<(() => void) | undefined>;
  triggerRequestData?: (data: () => TEntity[]) => void;
}

export default function DataTableComponent<TEntity extends DataTableValue>({
  controller,
  dataTable,
  dataTableColumns,
  enableGridRowActions = false,
  enableAddAction = false,
  editMode,
  filterDisplay,
  onButtonClick,
  onAfterDataLoaded,
  triggerRefreshData,
  triggerRequestData,
}: IField<TEntity>) {
  const [loading, setLoading] = useState(true);
  const [dataTableDto, setDataTableDto] =
    useState<DataTableDto<TEntity>>(dataTable);

  const dataTableService = new DataTableService(
    controller,
    dataTableDto,
    setDataTableDto,
    setLoading,
    null,
    onAfterDataLoaded
  );
  // Return data to parent.
  if (triggerRequestData) triggerRequestData(() => dataTableDto.data);

  React.useEffect(() => {
    if (triggerRefreshData)
      triggerRefreshData.current = dataTableService.refreshData;
  }, [triggerRefreshData]);

  React.useEffect(() => {
    dataTableService.loadData(null);
    console.log("loaded");
  }, []);

  const getDataTableColumns = () => {
    let canAddAction =
      dataTableColumns.filter((x) => x.header === "Actions").length === 0;

    canAddAction = canAddAction && enableGridRowActions;

    if (canAddAction)
      dataTableColumns.push({
        field: "",
        header: "Actions",
        sortable: false,
        filter: false,
        filterPlaceholder: "",
        style: { width: "20%" },
        body: gridRowActions,
      });
    return dataTableColumns;
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
          onClick={() => onButtonClick(ButtonTypeEnum.ADD)}
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
        rows={dataTableDto.rows}
        totalRecords={dataTableDto.pageCount}
        onPage={dataTableService.onPage}
        rowsPerPageOptions={[10, 25, 50, 100]}
        // paginatorLeft={paginatorLeft}
        currentPageReportTemplate={
          "1 to " + dataTableDto.rows + " out of " + dataTableDto.pageCount
        }
        paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
        // Filter.
        filterDisplay={filterDisplay}
        filters={dataTableDto.filters}
        onFilter={dataTableService.onFilter}
        // Sort.
        removableSort
        sortMode="multiple"
        onSort={dataTableService.onSort}
        multiSortMeta={dataTableDto.multiSortMeta}
        header={enableAddAction ? renderHeader() : null}
        // Edit row/column.
        editMode={editMode}
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
            editor={col.editor}
            onCellEditComplete={
              col.onCellEditComplete
                ? col.onCellEditComplete
                : dataTableService.onCellEditComplete
            }
            onCellEditInit={col.onCellEditInit}
          ></Column>
        ))}
      </DataTable>
    </>
  );
}
