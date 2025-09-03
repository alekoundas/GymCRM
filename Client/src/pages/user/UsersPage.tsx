import { useRef, useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import UserForm from "./UserForm";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { UserDto } from "../../model/entities/user/UserDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useUserStore } from "../../stores/UserStore";
import ApiService from "../../services/ApiService";

export default function UsersPage() {
  const { userDto, setUserDto, resetUserDto } = useUserStore();
  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
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
  const [datatableDto, setDatatableDto] = useState<DataTableDto<UserDto>>({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    filters: [],
    dataTableSorts: [],
    // dataTableFilters: {
    //   userName: { value: "", matchMode: "contains" },
    //   email: { value: "", matchMode: "contains" },
    // },
  });

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "id",
      header: "Id",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "userName",
      header: "User Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "email",
      header: "Email",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
    },
    {
      field: "roleId",
      header: "Role Id",
      sortable: true,
      filter: true,
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
        resetUserDto();
        setAddDialogVisibility(true);
        break;
      case ButtonTypeEnum.EDIT:
        setEditDialogVisibility(true);
        break;
      case ButtonTypeEnum.DELETE:
        // setDeleteDialogVisibility(true);
        break;

      default:
        break;
    }
  };

  const OnSaveAdd = async (): Promise<void> => {
    const response = await ApiService.create("users", userDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }

    return Promise.resolve();
  };

  const OnSaveEdit = async (): Promise<void> => {
    const response = await ApiService.update("users", userDto, userDto.id);

    if (response) {
      dialogControlEdit.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
    return Promise.resolve();
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
      {/*           View Train Group           */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <UserForm formMode={FormMode.VIEW} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <UserForm formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <UserForm formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent>

      {/* Delete Modal */}
      {/* <DeleteDialogComponent
        onAfterRowDeletion={afterSave}
        triggerDialogVisibility={(fn) => (setDeleteDialogVisibility = fn)}
        id={userDto.id}
        name={userDto.userName}
      /> */}
    </>
  );
}
