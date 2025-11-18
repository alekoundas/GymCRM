import { useEffect, useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogChildProps,
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { TokenService } from "../../services/TokenService";
import { ColumnBodyOptions } from "primereact/column";
import { Tag } from "primereact/tag";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useTrainGroupAttendanceStore } from "../../stores/TrainGroupAttendanceStore";
import { TrainGroupAttendanceDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceDto";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import { DataTableFilterDto } from "../../model/datatable/DataTableFilterDto";

interface IField extends DialogChildProps {
  userId?: string;
  trainGroupId?: number;
}

export default function TrainGroupAttendanceGridComponent({
  formMode,
  userId,
  trainGroupId,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { trainGroupAttendanceDto, setTrainGroupAttendanceDto } =
    useTrainGroupAttendanceStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupAttendanceDto>) => void) | undefined
  >(undefined);

  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const getFilters = () => {
    let filters: DataTableFilterDto[] = [];
    if (userId !== undefined)
      filters = [
        {
          fieldName: "UserId",
          value: userId,
          filterType: "equals",
        },
      ];

    if (trainGroupId !== undefined)
      filters = [
        {
          fieldName: "TrainGroupId",
          value: trainGroupId.toString(),
          filterType: "equals",
        },
      ];

    return filters;
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupAttendanceDto>
  >({
    ...new DataTableDto(),
    filters: getFilters(),
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [ButtonTypeEnum.DELETE];
  };

  const dataTableColumns: DataTableColumns<TrainGroupAttendanceDto>[] = [
    {
      field: "attendanceDate",
      header: t("Attendance date"),
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData, options) => (
        <>
          {new Date(rowData.attendanceDate).getDate() +
            "/" +
            (new Date(rowData.attendanceDate).getMonth() + 1) +
            "/" +
            new Date(rowData.attendanceDate).getFullYear()}
        </>
      ),
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "userId",
      header: t("Recipient"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData, options) => chipTemplate(rowData.user),
      style: { width: "20%" },
    },
  ];

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto | undefined) => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = "data:image/png;base64," + user.profileImage;
      return (
        <div className="flex m-0 p-0 align-items-center">
          <Avatar
            image={user.profileImage ? imageSrc : ""}
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="normal"
            className=" mr-2 "
          />
          {" " +
            user.firstName[0].toUpperCase() +
            user.firstName.slice(1, user.firstName.length) +
            " " +
            user.lastName[0].toUpperCase() +
            user.lastName.slice(1, user.lastName.length)}
        </div>
      );
    }
  };

  const onDelete = async (): Promise<void> => {
    if (trainGroupAttendanceDto.id) {
      const response = await apiService.delete(
        "TrainGroupAttendances",
        trainGroupAttendanceDto.id
      );

      dialogControlDelete.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupAttendanceDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        break;
      case ButtonTypeEnum.ADD:
        break;
      case ButtonTypeEnum.EDIT:
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setTrainGroupAttendanceDto(rowData);
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
        controller="TrainGroupAttendances"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                       */}
      {/*          Delete Train Group           */}
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
