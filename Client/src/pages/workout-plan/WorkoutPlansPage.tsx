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
import { TokenService } from "../../services/TokenService";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { Card } from "primereact/card";
import { Tag } from "primereact/tag";
import { ColumnFilterElementTemplateOptions } from "primereact/column";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import WorkoutPlanActivationDialogComponent, {
  WorkoutPlanActivationMode,
} from "./WorkoutPlanActivationDialogComponent";

export default function WorkoutPlansPage() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const navigate = useNavigate();
  const location = useLocation();
  const isAdminPage = location.pathname.includes("/administrator");
  const { workoutPlanDto, resetWorkoutPlanDto, setWorkoutPlanDto } =
    useWorkoutPlanStore();

  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  // The plan being deactivated or activated, and which of the two.
  const [activationPlan, setActivationPlan] = useState<WorkoutPlanDto | undefined>(undefined);
  const [activationMode, setActivationMode] =
    useState<WorkoutPlanActivationMode>("DEACTIVATE");

  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanDto>) => void) | undefined
  >(undefined);

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<WorkoutPlanDto>
  >(() => {
    // Read userId from URL if it exists (comma-separated array as string)
    // Links from the users grid still arrive comma separated; the shared url
    // reader repeats the parameter instead, and both are accepted here.
    const searchParams = new URLSearchParams(location.search);
    const userIdParam =
      searchParams.getAll("userId").length > 1
        ? searchParams.getAll("userId")
        : searchParams.get("userId")?.split(",");

    const baseFilters = [
      { fieldName: "createdOn", filterType: "between" as const },
      { fieldName: "title", filterType: "contains" as const },
    ];

    const userIdFilter = isAdminPage
      ? userIdParam
        ? {
            fieldName: "userId",
            filterType: "in" as const,
            values: userIdParam,
          }
        : { fieldName: "userId", filterType: "in" as const }
      : {
          fieldName: "userId",
          values: [TokenService.getUserId() ?? ""],
          filterType: "in" as const,
        };

    // The member page lists active plans only, for staff opening it as well.
    const activeOnlyFilters = isAdminPage
      ? []
      : [{ fieldName: "activeOnly", value: "true", filterType: "equals" as const }];

    return {
      ...new DataTableDto(),
      filters: [...baseFilters, userIdFilter, ...activeOnlyFilters],
      // Plans are never retired, so a member can hold dozens. Most recently trained
      // first beats most recently written by the trainer. sorts is what the server
      // orders by; dataTableSorts only draws the arrow.
      sorts: [{ fieldName: "lastRecordingOn", order: -1 }],
      dataTableSorts: [{ field: "lastRecordingOn", order: -1 }],
    };
  });


  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    const result: ButtonTypeEnum[] = [];
    if (isAdminPage) {
      const isView = TokenService.isUserAllowed("WorkoutPlansAdmin_View");
      if (isView) result.push(ButtonTypeEnum.VIEW);
      const isAdd = TokenService.isUserAllowed("WorkoutPlansAdmin_Add");
      if (isAdd) result.push(ButtonTypeEnum.ADD);
      if (isAdd) result.push(ButtonTypeEnum.CLONE);
      const isEdit = TokenService.isUserAllowed("WorkoutPlansAdmin_Edit");
      if (isEdit) result.push(ButtonTypeEnum.EDIT);
      if (isEdit) result.push(ButtonTypeEnum.DEACTIVATE);
      if (isEdit) result.push(ButtonTypeEnum.ACTIVATE);
      const isDelete = TokenService.isUserAllowed("WorkoutPlansAdmin_Delete");
      if (isDelete) result.push(ButtonTypeEnum.DELETE);
      return result;
    }

    const isEdit = TokenService.isUserAllowed("WorkoutPlans_Edit");
    if (isEdit) result.push(ButtonTypeEnum.EDIT);
    if (isEdit) result.push(ButtonTypeEnum.DEACTIVATE);
    return result;
  };

  // Deactivate on an active plan, Activate on an inactive one.
  const isGridRowButtonVisible = (
    buttonType: ButtonTypeEnum,
    rowData: WorkoutPlanDto
  ): boolean => {
    if (buttonType === ButtonTypeEnum.DEACTIVATE) return !rowData.isInactive;
    if (buttonType === ButtonTypeEnum.ACTIVATE) return !!rowData.isInactive;
    return true;
  };

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto | undefined) => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0,
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
          <Tag
            className="opacity-100"
            style={{
              backgroundColor: "#" + user.userStatus?.color,
            }}
          >
            {" " +
              user.firstName[0].toUpperCase() +
              user.firstName.slice(1, user.firstName.length) +
              " " +
              user.lastName[0].toUpperCase() +
              user.lastName.slice(1, user.lastName.length)}
          </Tag>
        </div>
      );
    }
  };

  const dataTableColumns: DataTableColumns<WorkoutPlanDto>[] = [
    {
      field: "title",
      header: t("Title"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "40%" },
    },
    ...(isAdminPage
      ? [
          {
            field: "userId",
            header: t("Participant"),
            sortable: true,
            filter: true,
            filterPlaceholder: t("Search"),
            filterTemplate: (options: ColumnFilterElementTemplateOptions) =>
              isAdminPage ? (
                <DataTableFilterIdComponent
                  options={options}
                  controller="users"
                />
              ) : (
                <></>
              ),
            body: (rowData: { user: UserDto | undefined }, options: any) =>
              chipTemplate(rowData.user),
            style: { width: "10%" },
          },
        ]
      : []),
    {
      field: "currentWeek",
      header: t("Week"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "10%" },
      body: (rowData: WorkoutPlanDto) =>
        rowData.currentWeek ? (
          <Tag value={t("Week") + " " + rowData.currentWeek} />
        ) : (
          <span className="text-color-secondary">
            {rowData.workoutPlanRuleId ? "—" : t("No rule")}
          </span>
        ),
    },
    {
      // Computed from the recordings rather than stored - the controller claims
      // this field name and sorts it itself.
      field: "lastRecordingOn",
      header: t("Last used"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "12%" },
      body: (rowData: WorkoutPlanDto) =>
        rowData.lastRecordingOn ? (
          <span>
            {new Date(rowData.lastRecordingOn).getDate() +
              "/" +
              (new Date(rowData.lastRecordingOn).getMonth() + 1) +
              "/" +
              new Date(rowData.lastRecordingOn).getFullYear()}
          </span>
        ) : (
          <span className="text-color-secondary">{t("Never")}</span>
        ),
    },
    {
      field: "isRunning",
      header: t("Status"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "10%" },
      body: (rowData: WorkoutPlanDto) => {
        if (rowData.isRunning)
          return (
            <Tag
              severity="success"
              value={t("In progress")}
            />
          );
        if (rowData.hasIncompleteRecording)
          return (
            <Tag
              severity="warning"
              value={t("Left open")}
            />
          );
        return <span className="text-color-secondary">—</span>;
      },
    },
    // Only the admin ever sees an inactive plan, so only the admin needs the column.
    ...(isAdminPage
      ? [
          {
            field: "isInactive",
            header: t("Active"),
            sortable: true,
            filter: false,
            filterPlaceholder: "",
            style: { width: "8%" },
            body: (rowData: WorkoutPlanDto) =>
              rowData.isInactive ? (
                <Tag
                  severity="secondary"
                  icon="pi pi-eye-slash"
                  value={t("Inactive")}
                />
              ) : (
                <Tag
                  severity="success"
                  icon="pi pi-check"
                  value={t("Active")}
                />
              ),
          } as DataTableColumns<WorkoutPlanDto>,
        ]
      : []),
    {
      field: "createdOn",
      header: t("CreatedOn"),
      sortable: true,
      filter: true,
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
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
  ];

  // Everything the trainer wrote is worth keeping - the exercises, their grouping,
  // the notes. What belongs to a person is not: the name and the member, and the
  // rule with its week, which track how far that member got and mean nothing here.
  const cloneWorkoutPlan = async (workoutPlanId: number) => {
    // The grid rows carry no exercises, so the plan has to be read in full first.
    const source = await apiService.get<WorkoutPlanDto>(
      "WorkoutPlans",
      workoutPlanId.toString(),
    );

    if (!source) return;

    setWorkoutPlanDto({
      ...new WorkoutPlanDto(),
      title: "",
      userId: "",
      description: source.description,
      isCircular: source.isCircular,
      // Detached from the plan they came from, so the save creates new rows rather
      // than moving the originals - and each given its own temporary id. The list
      // identifies a row by id everywhere: the react key, the edits, the deletes,
      // the reordering. Cloning them all as 0 made every row the same row, so one
      // edit rewrote the lot. Negative to match what adding an exercise produces.
      exercises: (source.exercises ?? []).map((x, index) => ({
        ...x,
        id: (index + 1) * -1,
        workoutPlanId: 0,
      })),
    });

    navigate("add");
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: WorkoutPlanDto,
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          navigate(rowData.id + "/view");
        }
        break;
      case ButtonTypeEnum.ADD:
        resetWorkoutPlanDto();
        navigate("add");
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          navigate(rowData.id + "/edit");
        }
        break;
      case ButtonTypeEnum.CLONE:
        if (rowData) {
          cloneWorkoutPlan(rowData.id);
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setWorkoutPlanDto(rowData);
          dialogControlDelete.showDialog();
        }
        break;
      case ButtonTypeEnum.DEACTIVATE:
      case ButtonTypeEnum.ACTIVATE:
        if (rowData) {
          setActivationMode(
            buttonType === ButtonTypeEnum.ACTIVATE ? "ACTIVATE" : "DEACTIVATE"
          );
          setActivationPlan(rowData);
        }
        break;

      default:
        break;
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await apiService
      .delete("WorkoutPlans", workoutPlanDto.id)
      .then(() => {
        dialogControlDelete.hideDialog();
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      });
  };

  return (
    <>
      <Card title={t("Workout Plans")}>
        <DataTableComponent
          isUrlStateEnabled
          // A member's own id is forced by the page and re-forced by the server,
          // so it is noise in a link rather than something worth remembering.
          urlStateExcludedFields={isAdminPage ? [] : ["userId", "activeOnly"]}
          dataTableDto={datatableDto}
          setDataTableDto={setDatatableDto}
          formMode={FormMode.EDIT}
          onButtonClick={onDataTableClick}
          controller="WorkoutPlans"
          filterDisplay={DataTableFilterDisplayEnum.ROW}
          dataTableColumns={dataTableColumns}
          triggerRefreshData={triggerRefreshDataTable}
          availableGridRowButtons={availableGridRowButtons()}
          isGridRowButtonVisible={isGridRowButtonVisible}
        />
      </Card>

      <WorkoutPlanActivationDialogComponent
        plan={activationPlan}
        mode={activationMode}
        isAdminPage={isAdminPage}
        onHide={() => setActivationPlan(undefined)}
        onDone={() => {
          setActivationPlan(undefined);
          if (triggerRefreshDataTable.current)
            triggerRefreshDataTable.current(datatableDto);
        }}
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
