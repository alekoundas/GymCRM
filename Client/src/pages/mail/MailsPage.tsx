import { useRef, useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import ApiService from "../../services/ApiService";

import { useMailStore } from "../../stores/MailStore";
import { MailDto } from "../../model/entities/mail/MailDto";
import MailFormComponent from "./MailFormComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";

export default function MailsPage() {
  const { mailDto, setMailDto, resetMailDto, updateMailDto } = useMailStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<MailDto>) => void) | undefined
  >(undefined);

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisibility(true),
    hideDialog: () => setViewDialogVisibility(false),
  };
  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddDialogVisibility(true),
    hideDialog: () => setAddDialogVisibility(false),
  };
  const dialogControlEdit: DialogControl = {
    showDialog: () => setEditDialogVisibility(true),
    hideDialog: () => setEditDialogVisibility(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<DataTableDto<MailDto>>({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    filters: [
      { fieldName: "id", filterType: "equals" },
      { fieldName: "subject", filterType: "contains" },
      { fieldName: "userId", filterType: "in" },
      { fieldName: "createdOn", filterType: "between" },
    ],
    dataTableSorts: [],
  });

  const dataTableColumns: DataTableColumns<MailDto>[] = [
    {
      field: "id",
      header: "Id",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
    },
    {
      field: "subject",
      header: "Subject",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
    },
    {
      field: "userId",
      header: "Recipient",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      style: { width: "20%" },
    },
    {
      field: "createdOn",
      header: "CreatedOn",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData, options) => (
        <>
          {new Date(rowData.createdOn).getDate() +
            "/" +
            (new Date(rowData.createdOn).getMonth() + 1) +
            "/" +
            new Date(rowData.createdOn).getFullYear() +
            " " +
            new Date(rowData.createdOn).getHours() +
            ":" +
            new Date(rowData.createdOn).getMinutes()}
        </>
      ),

      style: { width: "10%" },
    },
  ];

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: any) => {
    if (rowData) setMailDto({ ...rowData });
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        setViewDialogVisibility(true);
        break;
      case ButtonTypeEnum.ADD:
        resetMailDto();
        setAddDialogVisibility(true);
        break;
      case ButtonTypeEnum.EDIT:
        setEditDialogVisibility(true);
        break;
      case ButtonTypeEnum.DELETE:
        setDeleteDialogVisibility(true);
        break;

      default:
        break;
    }
  };

  const OnSaveAdd = async () => {
    const response = await ApiService.create("mails", MailDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetMailDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const OnSaveEdit = async () => {
    const response = await ApiService.update(
      "mails",
      mailDto,
      mailDto.id ?? -1
    );

    if (response) {
      dialogControlEdit.hideDialog();
      resetMailDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await ApiService.delete("mails", mailDto.id ?? -1);

    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  return (
    <>
      <Card title="Email History">
        <div className="card">
          <DataTableComponent
            controller="mails"
            dataTableDto={datatableDto}
            setDataTableDto={setDatatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={false}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={triggerRefreshDataTable}
            authorize={true}
          />
        </div>
      </Card>

      {/*                                    */}
      {/*        View Email History          */}
      {/*                                    */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <MailFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                   */}
      {/*          Add Email History        */}
      {/*                                   */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
        formMode={FormMode.ADD}
      >
        <div className="w-full">
          <MailFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                  */}
      {/*        Edit Email History        */}
      {/*                                  */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
        formMode={FormMode.EDIT}
      >
        <div className="w-full">
          <MailFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Delete Email History         */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDelete}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>Are you sure?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
