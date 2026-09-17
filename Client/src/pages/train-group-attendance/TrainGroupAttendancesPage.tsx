import { useRef, useState } from "react";
import { Card } from "primereact/card";
import { Avatar } from "primereact/avatar";
import { Tag } from "primereact/tag";
import { Button } from "primereact/button";
import { TokenService } from "../../services/TokenService";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { TrainGroupAttendanceDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceDto";
import { TrainGroupAttendanceAddDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceAddDto";
import TrainGroupAttendanceAddFormComponent, {
  AttendanceSource,
} from "./TrainGroupAttendanceAddFormComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function TrainGroupAttendancesPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupAttendanceDto>) => void) | undefined
  >(undefined);

  const [selected, setSelected] = useState<TrainGroupAttendanceDto>(
    new TrainGroupAttendanceDto()
  );
  const [addDto, setAddDto] = useState<TrainGroupAttendanceAddDto>(
    new TrainGroupAttendanceAddDto()
  );
  const [source, setSource] = useState<AttendanceSource>("GROUP");

  const [isAddDialogVisible, setAddDialogVisibility] = useState(false);
  const [isViewDialogVisible, setViewDialogVisibility] = useState(false);
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false);

  const dialogControlAdd: DialogControl = {
    showDialog: () => {
      setAddDto(new TrainGroupAttendanceAddDto());
      setSource("GROUP");
      setAddDialogVisibility(true);
    },
    hideDialog: () => setAddDialogVisibility(false),
  };
  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisibility(true),
    hideDialog: () => setViewDialogVisibility(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupAttendanceDto>
  >({
    ...new DataTableDto(),
    rows: 10,
    filters: [
      { fieldName: "attendanceDate", filterType: "between" },
      { fieldName: "userId", filterType: "in" },
      { fieldName: "trainGroupTitle", filterType: "contains" },
      { fieldName: "trainerFullName", filterType: "contains" },
    ],
    dataTableSorts: [{ field: "attendanceDate", order: -1 }],
  });

  const formatDate = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  // A stored wall clock, shown as the clock it is.
  const formatClock = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return (
      date.getUTCHours().toString().padStart(2, "0") +
      ":" +
      date.getUTCMinutes().toString().padStart(2, "0")
    );
  };

  const memberTemplate = (user: UserDto | undefined) => {
    if (!user) return <></>;

    const initials = `${user.firstName?.charAt(0) ?? ""}${
      user.lastName?.charAt(0) ?? ""
    }`.toUpperCase();

    return (
      <div className="flex m-0 p-0 align-items-center">
        <Avatar
          image={
            user.profileImage ? "data:image/png;base64," + user.profileImage : ""
          }
          label={user.profileImage ? undefined : initials}
          shape="circle"
          size="normal"
          className="mr-2"
        />
        {user.firstName} {user.lastName}
      </div>
    );
  };

  const dataTableColumns: DataTableColumns<TrainGroupAttendanceDto>[] = [
    {
      field: "attendanceDate",
      header: t("Attendance date"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData) => formatDate(rowData.attendanceDate),
      style: { width: "12%" },
    },
    {
      field: "userId",
      header: t("User"),
      sortable: false,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData) => memberTemplate(rowData.user),
      style: { width: "20%" },
    },
    {
      // The snapshot, not the group: it outlives the group and says what the session
      // was on the day rather than what it has since been renamed to.
      field: "trainGroupTitle",
      header: t("Train Group"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      body: (rowData) => (
        <div className="flex align-items-center gap-2">
          <span>{rowData.trainGroupTitle}</span>
          {!rowData.trainGroupId && (
            <Tag
              severity="warning"
              value={t("Deleted")}
            />
          )}
        </div>
      ),
      style: { width: "22%" },
    },
    {
      field: "trainerFullName",
      header: t("Trainer"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "16%" },
    },
    {
      field: "trainGroupStartOn",
      header: t("Start On"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatClock(rowData.trainGroupStartOn),
      style: { width: "8%" },
    },
    {
      field: "trainGroupDuration",
      header: t("Duration"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatClock(rowData.trainGroupDuration),
      style: { width: "8%" },
    },
    {
      field: "createdOn",
      header: t("Recorded"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatDate(rowData.createdOn),
      style: { width: "10%" },
    },
  ];

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    const result: ButtonTypeEnum[] = [ButtonTypeEnum.VIEW];

    // ADD is what puts the button in the grid header, not on a row.
    if (TokenService.isUserAllowed("TrainGroups_Add"))
      result.push(ButtonTypeEnum.ADD);

    if (TokenService.isUserAllowed("TrainGroups_Delete"))
      result.push(ButtonTypeEnum.DELETE);

    return result;
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupAttendanceDto
  ) => {
    if (rowData) setSelected({ ...rowData });

    switch (buttonType) {
      case ButtonTypeEnum.ADD:
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.VIEW:
        dialogControlView.showDialog();
        break;
      case ButtonTypeEnum.DELETE:
        dialogControlDelete.showDialog();
        break;
      default:
        break;
    }
  };

  const refresh = () => {
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  const onSaveAdd = async (): Promise<void> => {
    // Whichever half of the form was not used is cleared, so the server is told
    // plainly which of the two this is rather than having to guess from leftovers.
    const payload: TrainGroupAttendanceAddDto =
      source === "GROUP"
        ? {
            ...addDto,
            trainGroupTitle: "",
            trainGroupDescription: "",
            trainGroupStartOn: undefined,
            trainGroupDuration: undefined,
            trainerFullName: "",
          }
        : { ...addDto, trainGroupId: undefined };

    const response = await apiService.create<TrainGroupAttendanceAddDto>(
      "TrainGroupAttendances",
      payload
    );

    if (response) {
      dialogControlAdd.hideDialog();
      refresh();
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await apiService.delete<TrainGroupAttendanceDto>(
      "TrainGroupAttendances",
      selected.id
    );

    if (response !== null) {
      dialogControlDelete.hideDialog();
      refresh();
    }
  };

  const viewRow = (label: string, value: string) => (
    <div className="col-12 sm:col-6">
      <div className="text-color-secondary text-sm mb-1">{label}</div>
      <div className="text-lg font-medium">{value || "—"}</div>
    </div>
  );

  return (
    <>
      <Card title={t("Attendances")}>
        <DataTableComponent
          controller="TrainGroupAttendances"
          isUrlStateEnabled
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

      <GenericDialogComponent
        header={t("New attendance")}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={onSaveAdd}
        formMode={FormMode.ADD}
      >
        <TrainGroupAttendanceAddFormComponent
          formMode={FormMode.ADD}
          source={source}
          setSource={setSource}
          dto={addDto}
          update={(updates) => setAddDto((prev) => ({ ...prev, ...updates }))}
        />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={selected.trainGroupTitle}
        visible={isViewDialogVisible}
        control={dialogControlView}
        formMode={FormMode.VIEW}
        footer={<></>}
      >
        <div className="grid">
          {viewRow(t("User"), `${selected.user?.firstName ?? ""} ${selected.user?.lastName ?? ""}`.trim())}
          {viewRow(t("Attendance date"), formatDate(selected.attendanceDate))}
          {viewRow(t("Start On"), formatClock(selected.trainGroupStartOn))}
          {viewRow(t("Duration"), formatClock(selected.trainGroupDuration))}
          {viewRow(t("Trainer"), selected.trainerFullName)}
          {viewRow(t("Description"), selected.trainGroupDescription)}
        </div>
      </GenericDialogComponent>

      <GenericDialogComponent
        header={`${t("Are you sure")}?`}
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        formMode={FormMode.VIEW}
        footer={
          <div className="flex justify-content-between align-items-center">
            <Button
              label={t("Cancel")}
              icon="pi pi-times"
              text
              onClick={() => dialogControlDelete.hideDialog()}
            />
            <Button
              label={t("Delete")}
              icon="pi pi-trash"
              severity="danger"
              onClick={onDelete}
            />
          </div>
        }
      >
        <div>
          <p className="m-0">
            {t(
              "This attendance will be removed and the member gets the lesson back"
            )}
            .
          </p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
