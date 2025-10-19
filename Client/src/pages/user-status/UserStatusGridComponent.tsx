import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import UserStatusFormComponent from "./UserStatusFormComponent";
import { useUserStatusStore } from "../../stores/UserStatusStore";
import { ColumnBodyOptions } from "primereact/column";
import { Tag } from "primereact/tag";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { UserStatusDto } from "../../model/entities/user-status/UserStatusDto";

export default function UserStatusGridComponent() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { userStatusDto, setUserStatusDto, resetUserStatusDto } =
    useUserStatusStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<UserStatusDto>) => void) | undefined
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
  const [datatableDto, setDatatableDto] = useState<DataTableDto<UserStatusDto>>(
    {
      ...new DataTableDto(),
      filters: [],
      dataTableSorts: [],
    }
  );

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.ADD,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
    ];
  };
  const hexToRgba = (
    hex: string | undefined,
    opacity: number = 0.1
  ): string => {
    // Default to a neutral color if hex is invalid
    if (!hex || !/^[0-9A-Fa-f]{6}$/.test(hex)) {
      return `rgba(128, 128, 128, ${opacity})`; // Fallback: gray with specified opacity
    }

    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
  };
  const dataTableColumns: DataTableColumns<UserStatusDto>[] = [
    {
      field: "name",
      header: t("Name"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "40%" },
    },
    {
      field: "color",
      header: t("Color"),
      sortable: true,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
      body: (rowData: UserStatusDto, options: ColumnBodyOptions) => {
        if (rowData.color)
          return (
            <Tag
              className="p-2 opacity-50 w-full"
              style={{
                backgroundColor: "#" + rowData.color,
              }}
            ></Tag>
          );
      },
    },
  ];

  const OnSaveAdd = async (): Promise<void> => {
    const response = await apiService.create("UserStatuses", userStatusDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetUserStatusDto();

      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (userStatusDto.id) {
      const response = await apiService.update(
        "UserStatuses",
        userStatusDto,
        userStatusDto.id
      );

      if (response) {
        dialogControlEdit.hideDialog();
        setUserStatusDto(new UserStatusDto());

        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      }
    }
  };

  const onDelete = async (): Promise<void> => {
    if (userStatusDto.id) {
      const response = await apiService.delete(
        "UserStatuses",
        userStatusDto.id
      );

      dialogControlDelete.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: UserStatusDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setUserStatusDto(rowData);
          dialogControlView.showDialog();
        }
        break;
      case ButtonTypeEnum.ADD:
        resetUserStatusDto();
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setUserStatusDto(rowData);
          dialogControlEdit.showDialog();
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setUserStatusDto(rowData);
          dialogControlDelete.showDialog();
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.EDIT}
        onButtonClick={onDataTableClick}
        controller="UserStatuses"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                      */}
      {/*           View User status           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <UserStatusFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add User status           */}
      {/*                                     */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <UserStatusFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit User status           */}
      {/*                                     */}
      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <UserStatusFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Delete User status           */}
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
    </>
  );
}
