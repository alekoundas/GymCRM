import { useRef, useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import UserForm from "./UserForm";
import { UserDto } from "../../model/UserDto";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import AddDialogComponent from "../../components/core/dialog/AddDialogComponent";
import DeleteDialogComponent from "../../components/core/dialog/DeleteDialogComponent";
import EditDialogComponent from "../../components/core/dialog/EditDialogComponent";
import ViewDialogComponent from "../../components/core/dialog/ViewDialogComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";

export default function Users() {
  const [userDto, setUserDto] = useState<UserDto>(new UserDto());

  let setDeleteDialogVisibility: (newValue: boolean) => void = () => {};
  let setEditDialogVisibility: (newValue: boolean) => void = () => {};
  let setViewDialogVisibility: (newValue: boolean) => void = () => {};
  let setAddDialogVisibility: (newValue: boolean) => void = () => {};
  let setSaveDisable: (newValue: boolean) => void = () => {};
  let triggerFormSave: () => void = () => {};
  const onRefreshDataTable = useRef<(() => void) | undefined>(undefined);

  const formMode: FormMode = window.location.href.endsWith("/add")
    ? FormMode.ADD
    : window.location.href.endsWith("/edit")
    ? FormMode.EDIT
    : FormMode.VIEW;

  const datatableDto: DataTableDto<UserDto> = {
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    dataTableSorts: [],
    dataTableFilters: {
      userName: { value: "", matchMode: "contains" },
      email: { value: "", matchMode: "contains" },
    },
  };

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "userName",
      header: "User Name",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "email",
      header: "Email",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "roleId",
      header: "Role Id",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
  ];

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: UserDto) => {
    if (rowData) setUserDto({ ...rowData });

    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        setViewDialogVisibility(true);
        break;
      case ButtonTypeEnum.ADD:
        setAddDialogVisibility(true);
        break;
      case ButtonTypeEnum.EDIT:
        setEditDialogVisibility(true);
        break;
      case ButtonTypeEnum.DELETE:
        setDeleteDialogVisibility(true);
        break;
      case ButtonTypeEnum.SAVE:
        triggerFormSave();
        setAddDialogVisibility(false);
        setEditDialogVisibility(false);
        if (onRefreshDataTable.current) onRefreshDataTable.current();
        break;

      default:
        break;
    }
  };

  const afterSave = () => {
    if (onRefreshDataTable.current) onRefreshDataTable.current();
    setDeleteDialogVisibility(false);
  };

  // Link onSave event button to form Save function.
  const [triger, settriger] = useState(0);
  let triggerSave = () => {
    settriger(triger + 1);
    triggerFormSave();
    setAddDialogVisibility(false);
    setEditDialogVisibility(false);
  };

  return (
    <>
      <Card title="Users">
        <DataTableComponent
          formMode={FormMode.EDIT}
          onButtonClick={onDataTableClick}
          controller="users"
          enableGridRowActions={true}
          enableAddAction={false}
          filterDisplay={DataTableFilterDisplayEnum.ROW}
          dataTable={datatableDto}
          dataTableColumns={dataTableColumns}
          triggerRefreshData={onRefreshDataTable}
        />
      </Card>

      {/* Delete Modal */}
      <DeleteDialogComponent
        onAfterRowDeletion={afterSave}
        triggerDialogVisibility={(fn) => (setDeleteDialogVisibility = fn)}
        id={userDto.id}
        name={userDto.userName}
      />

      {/* View Modal */}
      <ViewDialogComponent
        triggerDialogVisibility={(fn) => (setViewDialogVisibility = fn)}
      >
        <UserForm
          parentUserDto={userDto}
          formMode={FormMode.VIEW}
          onAfterSave={afterSave}
        />
      </ViewDialogComponent>

      {/* Edit Modal */}
      <EditDialogComponent
        onSaveButtonClick={triggerSave}
        triggerDialogVisibility={(fn) => (setEditDialogVisibility = fn)}
        triggerSaveDisable={(fn) => (setSaveDisable = fn)}
        triggerSaveEnable={(fn) => (setSaveDisable = fn)}
      >
        <UserForm
          parentUserDto={userDto}
          formMode={FormMode.EDIT}
          onAfterSave={afterSave}
          onDisableSaveButton={() => setSaveDisable(false)}
          onEnableSaveButton={() => setSaveDisable(true)}
          triggerSaveForm={triggerFormSave}
        />
      </EditDialogComponent>

      {/* Add Modal */}
      <AddDialogComponent
        onSaveButtonClick={triggerSave}
        triggerDialogVisibility={(fn) => (setAddDialogVisibility = fn)}
        triggerSaveDisable={(fn) => (setSaveDisable = fn)}
        triggerSaveEnable={(fn) => (setSaveDisable = fn)}
      >
        <UserForm
          parentUserDto={userDto}
          formMode={FormMode.ADD}
          onAfterSave={afterSave}
          onDisableSaveButton={() => setSaveDisable(false)}
          onEnableSaveButton={() => setSaveDisable(true)}
          triggerSaveForm={triggerFormSave}
        />
      </AddDialogComponent>
    </>
  );
}
