import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Card } from "primereact/card";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { DataTable } from "primereact/datatable";
import { CustomerDto } from "../../model/CustomerDto";
import { DataTableDto } from "../../model/DataTableDto";
import { Calendar } from "primereact/calendar";
import DataTableService from "../../services/DataTableService";

function Customers() {
  const navigate = useNavigate();
  const defaultUrlSearchQuery =
    "eyJkYXRhIjpbXSwicm93cyI6MTAsInBhZ2UiOjEsInBhZ2VDb3VudCI6MTEsIm11bHRpU29ydE1ldGEiOlt7ImZpZWxkIjoibGFzdE5hbWUiLCJvcmRlciI6MX0seyJmaWVsZCI6ImZpcnN0TmFtZSIsIm9yZGVyIjoxfSx7ImZpZWxkIjoiY3JlYXRlZE9uIiwib3JkZXIiOi0xfV0sImZpbHRlcnMiOnsiZmlyc3ROYW1lIjp7InZhbHVlIjoiIn0sImxhc3ROYW1lIjp7InZhbHVlIjoiIn0sImNyZWF0ZWRPbiI6eyJ2YWx1ZSI6IiJ9fX0=";
  const [UrlParameters, _setUrlParameters] = useSearchParams();

  const [loading, setLoading] = useState(true);
  const [dataTableDtoState, setDataTableDtoState] = useState<
    DataTableDto<CustomerDto>
  >({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    multiSortMeta: [
      { field: "lastName", order: 1 },
      { field: "firstName", order: 1 },
      { field: "createdOn", order: -1 },
    ],
    filters: {
      firstName: { value: "", matchMode: "contains" },
      lastName: { value: "", matchMode: "contains" },
      createdOn: { value: "", matchMode: "contains" },
    },
  });
  const dataTableService = new DataTableService(
    "customers",
    dataTableDtoState,
    setDataTableDtoState,
    setLoading,
    defaultUrlSearchQuery
  );

  useEffect(() => {
    var searchUrlParameter = UrlParameters.get("search");
    dataTableService.loadData(searchUrlParameter);
  }, []);

  const activityRowFilterTemplate = (options: any) => {
    return (
      <div className="flex gap-1">
        <Calendar
          value={options.value}
          onChange={(e) => options.filterApplyCallback(e.value)}
          selectionMode="range"
          // readOnlyInput
          className="w-full"
          hideOnRangeSelection
        />
      </div>
    );
  };

  const gridRowActions = (rowData: CustomerDto) => (
    <React.Fragment>
      <Button
        icon="pi pi-eye"
        rounded
        outlined
        onClick={() => navigate("/customers/" + rowData.id + "/view")}
      />
      <Button
        icon="pi pi-pencil"
        rounded
        outlined
        className="mr-2"
        severity="info"
        onClick={() => navigate("/customers/" + rowData.id + "/edit")}
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
          onClick={() => navigate("/customers/add")}
        />
      </div>
    );
  };

  return (
    <>
      <Card title="Customers">
        <DataTable
          value={dataTableDtoState.data}
          lazy
          stripedRows
          emptyMessage="No customers found."
          tableStyle={{ minWidth: "50rem" }}
          selectionMode="single"
          loading={loading}
          header={renderHeader}
          // Pagging.
          paginator
          rows={dataTableDtoState.rows}
          totalRecords={dataTableDtoState.pageCount}
          onPage={dataTableService.onPage}
          rowsPerPageOptions={[10, 25, 50, 100]}
          // paginatorLeft={paginatorLeft}
          currentPageReportTemplate={
            "1 to " +
            dataTableDtoState.rows +
            " out of " +
            dataTableDtoState.pageCount
          }
          paginatorTemplate="FirstPageLink PrevPageLink CurrentPageReport NextPageLink LastPageLink RowsPerPageDropdown"
          // Filter.
          filterDisplay="row"
          filters={dataTableDtoState.filters}
          onFilter={dataTableService.onFilter}
          // Sort.
          removableSort
          sortMode="multiple"
          onSort={dataTableService.onSort}
          multiSortMeta={dataTableDtoState.multiSortMeta}
        >
          <Column
            field="firstName"
            header="First Name"
            sortable
            filter
            filterPlaceholder="Search by First Name"
          ></Column>
          <Column
            field="lastName"
            header="Last Name"
            sortable
            filter
            filterPlaceholder="Search by Last Name"
          ></Column>
          <Column
            field="createdOn"
            header="Creation Date"
            filterElement={activityRowFilterTemplate}
            sortable
            filter
            filterPlaceholder="Search by Creation Day"
          ></Column>
          <Column
            header="Actions"
            headerStyle={{ width: "20%", minWidth: "8rem" }}
            body={gridRowActions}
          ></Column>
        </DataTable>
      </Card>
    </>
  );
}

export default Customers;
