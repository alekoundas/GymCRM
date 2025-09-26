import { useRef, useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import UserFormComponent from "./UserFormComponent";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { UserDto } from "../../model/entities/user/UserDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useUserStore } from "../../stores/UserStore";
import ApiService from "../../services/ApiService";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";

export default function UsersPage() {
  const { userDto, setUserDto, resetUserDto } = useUserStore();
  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<UserDto>) => void) | undefined
  >(undefined);

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
  const [datatableDto, setDatatableDto] = useState<DataTableDto<UserDto>>({
    ...new DataTableDto(),
    filters: [
      // { fieldName: "id", filterType: "equals" },
      { fieldName: "subject", filterType: "contains" },
      { fieldName: "userId", filterType: "in" },
      { fieldName: "createdOn", filterType: "between" },
    ],
  });

  const dataTableColumns: DataTableColumns<UserDto>[] = [
    {
      field: "id",
      header: "Id",
      sortable: true,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "20%" },
    },
    {
      field: "userName",
      header: "User Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
    },
    {
      field: "email",
      header: "Email",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
    },
    {
      field: "roleId",
      header: "Role Id",
      sortable: false,
      filter: true,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="roles"
        />
      ),
      style: { width: "20%" },
    },
  ];

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: UserDto) => {
    if (rowData) setUserDto({ ...rowData });

    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        setViewDialogVisibility(true);
        break;
      case ButtonTypeEnum.ADD:
        resetUserDto();
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

  const onSaveAdd = async (): Promise<void> => {
    const response = await ApiService.create("users", userDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    const response = await ApiService.update("users", userDto, userDto.id);

    if (response) {
      dialogControlEdit.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await ApiService.delete("users", userDto.id);

    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  return (
    <>
      <Card title="Users">
        <DataTableComponent
          controller="users"
          dataTableDto={datatableDto}
          setDataTableDto={setDatatableDto}
          formMode={FormMode.EDIT}
          onButtonClick={onDataTableClick}
          enableGridRowActions={true}
          enableAddAction={false}
          filterDisplay={DataTableFilterDisplayEnum.ROW}
          dataTableColumns={dataTableColumns}
          triggerRefreshData={triggerRefreshDataTable}
        />
      </Card>

      {/*                                      */}
      {/*               View User              */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isViewDialogVisible}
        control={dialogControlView}
        formMode={FormMode.VIEW}
      >
        <div className="w-full">
          <UserFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*               Add User              */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={onSaveAdd}
        formMode={FormMode.ADD}
      >
        <div className="w-full">
          <UserFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*              Edit User              */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={onSaveEdit}
        formMode={FormMode.EDIT}
      >
        <div className="w-full">
          <UserFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*              Delete User              */}
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
