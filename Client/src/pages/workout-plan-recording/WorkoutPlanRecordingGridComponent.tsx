import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Tag } from "primereact/tag";
import { FormMode } from "../../enum/FormMode";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";
import { WorkoutPlanRecordingDto } from "../../model/entities/workout-plan-recording/WorkoutPlanRecordingDto";

const CONTROLLER = "WorkoutPlanRecordings";

interface IField {
  // When set the grid is scoped to a single plan - the dialog opened from the plan page.
  workoutPlanId?: number;
  isAdminPage: boolean;
  // Only the standalone page remembers its filters in the url. The same grid also
  // runs inside the plan dialog, which has no business touching the address bar.
  isUrlStateEnabled?: boolean;
}

// Seconds to h:mm:ss, or mm:ss when under an hour.
export const formatDuration = (seconds: number | undefined): string => {
  if (seconds === undefined || seconds === null) return "";

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  const pad = (value: number) => value.toString().padStart(2, "0");

  return hours > 0
    ? `${hours}:${pad(minutes)}:${pad(secs)}`
    : `${pad(minutes)}:${pad(secs)}`;
};

const formatDate = (value: string | undefined): string => {
  if (!value) return "";
  const date = new Date(value);
  return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
};

const formatTime = (value: string | undefined): string => {
  if (!value) return "";
  const date = new Date(value);
  return `${date.getHours()}:${date.getMinutes().toString().padStart(2, "0")}`;
};

export default function WorkoutPlanRecordingGridComponent({
  workoutPlanId,
  isAdminPage,
  isUrlStateEnabled = false,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const location = useLocation();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanRecordingDto>) => void) | undefined
  >(undefined);

  const [selectedRecording, setSelectedRecording] =
    useState<WorkoutPlanRecordingDto | null>(null);
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false);

  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<WorkoutPlanRecordingDto>
  >(() => {
    const filters = [
      { fieldName: "startedOn", filterType: "between" as const },
      { fieldName: "weekNumber", filterType: "equals" as const },
    ];

    // A column is only filterable if it is declared here: the datatable builds its
    // filter metadata from this list, so one marked filter={true} without an entry
    // renders a box that collects a value nothing ever reads. Shown only when the
    // column itself is, which is when the grid is not already scoped to one plan.
    if (workoutPlanId === undefined)
      filters.push({
        fieldName: "workoutPlanTitle",
        filterType: "contains" as const,
      } as never);

    // Scoped to one plan when opened from the plan page. The server also forces a
    // member to their own rows, so this is a view filter, not the security boundary.
    if (workoutPlanId !== undefined)
      filters.push({
        fieldName: "workoutPlanId",
        filterType: "equals" as const,
        ...{ value: String(workoutPlanId) },
      } as never);

    // Arriving from the users grid row action pre-filters to that member.
    if (isAdminPage) {
      const userIdParam = new URLSearchParams(location.search)
        .get("userId")
        ?.split(",");

      filters.push({
        fieldName: "userId",
        filterType: "in" as const,
        ...(userIdParam ? { values: userIdParam } : {}),
      } as never);
    }

    return {
      ...new DataTableDto(),
      filters,
      dataTableSorts: [{ field: "startedOn", order: -1 }],
    };
  });

  // A recording still running has no duration yet, so the grid carries on from the
  // elapsed time the server sent with the row. The clock is read each tick rather than
  // a counter incremented, so a tab that was asleep comes back showing the right time
  // instead of however many ticks it managed.
  const [now, setNow] = useState<number>(Date.now());
  const rowsArrivedAtRef = useRef<number>(Date.now());

  useEffect(() => {
    rowsArrivedAtRef.current = Date.now();
    setNow(Date.now());

    // Nothing running, nothing to tick for.
    if (!datatableDto.data?.some((x) => x.isRunning)) return;

    const interval = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(interval);
  }, [datatableDto.data]);

  const runningSeconds = (rowData: WorkoutPlanRecordingDto): number =>
    (rowData.elapsedSeconds ?? 0) +
    Math.floor((now - rowsArrivedAtRef.current) / 1000);

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    const result: ButtonTypeEnum[] = [];
    // View only for members - the log is not theirs to edit.
    if (isAdminPage && TokenService.isUserAllowed("WorkoutPlanRecordings_Delete"))
      result.push(ButtonTypeEnum.DELETE);
    return result;
  };

  const statusBody = (rowData: WorkoutPlanRecordingDto) => {
    if (rowData.isRunning)
      return (
        <Tag
          severity="success"
          value={t("In progress")}
        />
      );
    if (rowData.isIncomplete)
      return (
        <Tag
          severity="warning"
          value={t("Left open")}
        />
      );
    return <span className="text-color-secondary">{t("Completed")}</span>;
  };

  const dataTableColumns: DataTableColumns<WorkoutPlanRecordingDto>[] = [
    {
      field: "startedOn",
      header: t("Started"),
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
      body: (rowData: WorkoutPlanRecordingDto) => (
        <span>
          {formatDate(rowData.startedOn)} {formatTime(rowData.startedOn)}
        </span>
      ),
    },
    ...(workoutPlanId === undefined
      ? [
          {
            field: "workoutPlanTitle",
            header: t("Workout plan"),
            sortable: true,
            filter: true,
            filterPlaceholder: t("Search"),
            style: { width: "20%" },
          } as DataTableColumns<WorkoutPlanRecordingDto>,
        ]
      : []),
    ...(isAdminPage
      ? [
          {
            field: "userId",
            header: t("Participant"),
            sortable: true,
            filter: true,
            filterPlaceholder: t("Search"),
            filterTemplate: (options: ColumnFilterElementTemplateOptions) => (
              <DataTableFilterIdComponent
                options={options}
                controller="users"
              />
            ),
            body: (rowData: WorkoutPlanRecordingDto) => (
              <span>
                {rowData.user
                  ? `${rowData.user.firstName} ${rowData.user.lastName}`
                  : ""}
              </span>
            ),
            style: { width: "15%" },
          } as DataTableColumns<WorkoutPlanRecordingDto>,
        ]
      : []),
    {
      field: "weekNumber",
      header: t("Week"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
      body: (rowData: WorkoutPlanRecordingDto) => (
        <Tag
          className="opacity-90"
          value={`${t("Week")} ${rowData.weekNumber}`}
        />
      ),
    },
    {
      field: "completedOn",
      header: t("Finished"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "12%" },
      body: (rowData: WorkoutPlanRecordingDto) =>
        rowData.completedOn ? (
          <span>{formatTime(rowData.completedOn)}</span>
        ) : (
          <span className="text-color-secondary">—</span>
        ),
    },
    {
      field: "durationSeconds",
      header: t("Duration"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "12%" },
      // One still running shows how long it has been going. One that was never
      // stopped stays blank: there is no duration to show and inventing one would
      // corrupt the numbers.
      body: (rowData: WorkoutPlanRecordingDto) =>
        rowData.isRunning ? (
          <span className="font-semibold text-primary">
            {formatDuration(runningSeconds(rowData))}
          </span>
        ) : (
          <span className="font-semibold">
            {formatDuration(rowData.durationSeconds)}
          </span>
        ),
    },
    {
      field: "isRunning",
      header: t("Status"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "12%" },
      body: statusBody,
    },
  ];

  const onDelete = async (): Promise<void> => {
    if (!selectedRecording?.id) return;

    await apiService.delete(CONTROLLER, selectedRecording.id);
    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: WorkoutPlanRecordingDto,
  ) => {
    if (buttonType === ButtonTypeEnum.DELETE && rowData) {
      setSelectedRecording(rowData);
      dialogControlDelete.showDialog();
    }
  };

  return (
    <>
      <DataTableComponent
        isUrlStateEnabled={isUrlStateEnabled}
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.VIEW}
        onButtonClick={onDataTableClick}
        controller={CONTROLLER}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />

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
