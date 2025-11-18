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

interface IField extends DialogChildProps {
  userId?: number;
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

  // const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  // const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  // const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  // const dialogControlView: DialogControl = {
  //   showDialog: () => setViewDialogVisibility(true),
  //   hideDialog: () => setViewDialogVisibility(false),
  // };
  // const dialogControlAdd: DialogControl = {
  //   showDialog: () => setAddDialogVisibility(true),
  //   hideDialog: () => setAddDialogVisibility(false),
  // };
  // const dialogControlEdit: DialogControl = {
  //   showDialog: () => setEditDialogVisibility(true),
  //   hideDialog: () => setEditDialogVisibility(false),
  // };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };
  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupAttendanceDto>
  >({
    ...new DataTableDto(),
    filters: [],
    dataTableSorts: [],
  });

  useEffect(() => {
    if (userId !== undefined)
      datatableDto.filters = [
        {
          fieldName: "UserId",
          value: userId.toString(),
          filterType: "equals",
        },
      ];

    if (trainGroupId !== undefined)
      datatableDto.filters = [
        {
          fieldName: "TrainGroupId",
          value: trainGroupId.toString(),
          filterType: "equals",
        },
      ];

    setDatatableDto(datatableDto);
  }, []);

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.ADD,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
    ];
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
            new Date(rowData.attendanceDate).getFullYear() +
            " " +
            new Date(rowData.attendanceDate).getHours() +
            ":" +
            new Date(rowData.attendanceDate).getMinutes()}
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

  // const OnSaveAdd = async (): Promise<void> => {
  //   const response = await apiService.create(
  //     "TrainGroupAttendances",
  //     trainGroupAttendanceDto
  //   );

  //   if (response) {
  //     dialogControlAdd.hideDialog();
  //     resetTrainGroupAttendanceDto();

  //     if (triggerRefreshDataTable.current)
  //       triggerRefreshDataTable.current(datatableDto);
  //   }
  // };

  // const OnSaveEdit = async (): Promise<void> => {
  //   if (trainGroupAttendanceDto.id) {
  //     const response = await apiService.update(
  //       "TrainGroupAttendances",
  //       trainGroupAttendanceDto,
  //       trainGroupAttendanceDto.id
  //     );

  //     if (response) {
  //       dialogControlEdit.hideDialog();
  //       setTrainGroupAttendanceDto(new TrainGroupAttendanceDto());

  //       if (triggerRefreshDataTable.current)
  //         triggerRefreshDataTable.current(datatableDto);
  //     }
  //   }
  // };

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
        if (rowData) {
          // setTrainGroupAttendanceDto(rowData);
          // dialogControlView.showDialog();
        }
        break;
      case ButtonTypeEnum.ADD:
        // resetTrainGroupAttendanceDto();
        // dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          // setTrainGroupAttendanceDto(rowData);
          // dialogControlEdit.showDialog();
        }
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

      {/*           View Train Group           */}
      {/*                                      */}

      {/* <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <TrainGroupAttendanceFormComponent />
        </div>
      </GenericDialogComponent> */}

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      {/* <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupAttendanceFormComponent />
        </div>
      </GenericDialogComponent> */}

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      {/* <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupAttendanceFormComponent />
        </div>
      </GenericDialogComponent> */}

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
