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
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useNavigate } from "react-router-dom";
import { Tag } from "primereact/tag";
import TrainGroupAttendanceGridComponent from "../train-group-attendance/TrainGroupAttendanceGridComponent";

export default function UsersPage() {
  const { t } = useTranslator();
  const navigate = useNavigate();
  const apiService = useApiService();
  const { userDto, setUserDto, resetUserDto } = useUserStore();

  const [isAttendancesDialogVisible, setAttendancesDialogVisibility] =
    useState(false); // Dialog visibility
  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<UserDto>) => void) | undefined
  >(undefined);

  const dialogControlAttendances: DialogControl = {
    showDialog: () => setAttendancesDialogVisibility(true),
    hideDialog: () => setAttendancesDialogVisibility(false),
  };
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
      { fieldName: "firstName", filterType: "contains" },
      { fieldName: "lastName", filterType: "contains" },
      { fieldName: "email", filterType: "contains" },
      { fieldName: "roleId", filterType: "custom" },
      { fieldName: "userStatusId", filterType: "in" },
    ],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.PROFILE,
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
      ButtonTypeEnum.ATTENDANCES,
    ];
  };

  const dataTableColumns: DataTableColumns<UserDto>[] = [
    {
      field: "firstName",
      header: t("First Name"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "lastName",
      header: t("Last Name"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "email",
      header: "Email",
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "roleId",
      header: t("Roles"),
      sortable: false,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="roles"
        />
      ),
      body: (rowData, options) => {
        return rowData.userRoles[0]?.role?.name;
      },
      style: { width: "20%" },
    },
    {
      field: "userStatusId",
      header: t("User Statuses"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="UserStatuses"
        />
      ),
      body: (rowData, options) => {
        if (rowData.userStatus)
          return (
            <Tag
              className="p-2 opacity-100 w-full"
              style={{
                backgroundColor: "#" + rowData?.userStatus.color,
              }}
            >
              {rowData.userStatus?.name}
            </Tag>
          );
      },
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
      case ButtonTypeEnum.PROFILE:
        navigate("/administrator/users/" + rowData?.id + "/profile");
        break;
      case ButtonTypeEnum.ATTENDANCES:
        dialogControlAttendances.showDialog();
        break;

      default:
        break;
    }
  };

  const onSaveAdd = async (): Promise<void> => {
    const response = await apiService.create("users", userDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    const response = await apiService.update("users", userDto, userDto.id);

    if (response) {
      dialogControlEdit.hideDialog();
      resetUserDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await apiService.delete("users", userDto.id);

    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  return (
    <>
      <Card title={t("Users")}>
        <DataTableComponent
          controller="users"
          dataTableDto={datatableDto}
          setDataTableDto={setDatatableDto}
          formMode={FormMode.EDIT}
          onButtonClick={onDataTableClick}
          filterDisplay={DataTableFilterDisplayEnum.ROW}
          dataTableColumns={dataTableColumns}
          triggerRefreshData={triggerRefreshDataTable}
          availableGridRowButtons={availableGridRowButtons()}
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
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Edit Attendances          */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isAttendancesDialogVisible}
        control={dialogControlAttendances}
        formMode={FormMode.EDIT}
      >
        <div className="w-full">
          <TrainGroupAttendanceGridComponent userId={userDto.id} />
        </div>
      </GenericDialogComponent>
    </>
  );
}
