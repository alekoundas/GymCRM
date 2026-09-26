import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Avatar } from "primereact/avatar";
import { Card } from "primereact/card";
import { TabPanel, TabView } from "primereact/tabview";
import { Tag } from "primereact/tag";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { ExerciseHistoryDto } from "../../model/entities/exercise-history/ExerciseHistoryDto";
import { UserDto } from "../../model/entities/user/UserDto";
import { useTranslator } from "../../services/TranslatorService";

// What members train with, for the trainer writing their next plan: the exercises in
// their plans now, and every change made to an exercise's sets, reps or weight. A row
// opens the plan it belongs to.
export default function ExerciseHistoryAdminPage() {
  const { t } = useTranslator();
  const navigate = useNavigate();

  const currentRefresh = useRef<((dto: DataTableDto<ExerciseDto>) => void) | undefined>(undefined);
  const historyRefresh = useRef<((dto: DataTableDto<ExerciseHistoryDto>) => void) | undefined>(undefined);

  const [currentDto, setCurrentDto] = useState<DataTableDto<ExerciseDto>>({
    ...new DataTableDto(),
    rows: 25,
    filters: [
      { fieldName: "userId", filterType: "in" },
      { fieldName: "workoutPlanTitle", filterType: "contains" },
      { fieldName: "name", filterType: "contains" },
    ],
    sorts: [{ fieldName: "name", order: 1 }],
    dataTableSorts: [{ field: "name", order: 1 }],
  });

  const [historyDto, setHistoryDto] = useState<DataTableDto<ExerciseHistoryDto>>({
    ...new DataTableDto(),
    rows: 25,
    filters: [
      { fieldName: "createdOn", filterType: "between" },
      { fieldName: "userId", filterType: "in" },
      { fieldName: "workoutPlanTitle", filterType: "contains" },
      { fieldName: "name", filterType: "contains" },
    ],
    // sorts is what the server orders by; dataTableSorts only draws the arrow.
    sorts: [{ fieldName: "createdOn", order: -1 }],
    dataTableSorts: [{ field: "createdOn", order: -1 }],
  });

  const memberTemplate = (user: UserDto | undefined) => {
    if (!user) return <></>;
    const initials = `${user.firstName?.charAt(0) ?? ""}${user.lastName?.charAt(0) ?? ""}`.toUpperCase();

    return (
      <div className="flex align-items-center gap-2">
        <Avatar
          image={user.profileImage ? "data:image/png;base64," + user.profileImage : undefined}
          label={user.profileImage ? undefined : initials}
          shape="circle"
        />
        <span>
          {user.firstName} {user.lastName}
        </span>
      </div>
    );
  };

  const formatDate = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  // The three numbers a trainer compares, side by side.
  const loadTemplate = (row: { sets: string; reps: string; weight: string }) => (
    <div className="flex flex-wrap gap-2">
      <Tag
        severity="info"
        value={`${row.sets || "–"} × ${row.reps || "–"}`}
      />
      {row.weight && (
        <Tag
          severity="secondary"
          value={row.weight}
        />
      )}
    </div>
  );

  const memberColumn = {
    field: "userId",
    header: t("Member"),
    sortable: false,
    filter: true,
    filterPlaceholder: t("Search"),
    filterTemplate: (options) => (
      <DataTableFilterIdComponent
        options={options}
        controller="users"
      />
    ),
    style: { width: "22%" },
  } as DataTableColumns<ExerciseDto>;

  const currentColumns: DataTableColumns<ExerciseDto>[] = [
    { ...memberColumn, body: (row: ExerciseDto) => memberTemplate(row.user) },
    {
      field: "workoutPlanTitle",
      header: t("Workout plan"),
      sortable: false,
      filter: true,
      filterPlaceholder: t("Search"),
      body: (row: ExerciseDto) => (
        <div className="flex flex-wrap align-items-center gap-2">
          <span>{row.workoutPlanTitle}</span>
          {row.isWorkoutPlanInactive && (
            <Tag
              severity="secondary"
              value={t("Inactive")}
            />
          )}
        </div>
      ),
      style: { width: "24%" },
    },
    {
      field: "name",
      header: t("Exercise"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "30%" },
    },
    {
      field: "sets",
      header: t("Load"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      body: (row: ExerciseDto) => loadTemplate(row),
      style: { width: "24%" },
    },
  ];

  const historyColumns: DataTableColumns<ExerciseHistoryDto>[] = [
    {
      field: "createdOn",
      header: t("Date"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => <DataTableFilterDateComponent options={options} />,
      body: (row: ExerciseHistoryDto) => formatDate(row.createdOn),
      style: { width: "14%" },
    },
    {
      ...(memberColumn as unknown as DataTableColumns<ExerciseHistoryDto>),
      body: (row: ExerciseHistoryDto) => memberTemplate(row.user),
      style: { width: "20%" },
    },
    {
      field: "workoutPlanTitle",
      header: t("Workout plan"),
      sortable: false,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "name",
      header: t("Exercise"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "26%" },
    },
    {
      field: "sets",
      header: t("Load"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      body: (row: ExerciseHistoryDto) => loadTemplate(row),
      style: { width: "20%" },
    },
  ];

  return (
    <Card title={t("Exercise history")}>
      <TabView>
        <TabPanel
          header={t("Current exercises")}
          leftIcon="pi pi-list mr-2"
        >
          <DataTableComponent
            controller="Exercises"
            dataTableDto={currentDto}
            setDataTableDto={setCurrentDto}
            formMode={FormMode.VIEW}
            onButtonClick={() => {}}
            onRowClick={(row) => navigate(`/administrator/workout-plans/${row.workoutPlanId}/view`)}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            dataTableColumns={currentColumns}
            triggerRefreshData={currentRefresh}
            isUrlStateEnabled
            urlStateKey="current"
          />
        </TabPanel>

        <TabPanel
          header={t("History")}
          leftIcon="pi pi-history mr-2"
        >
          <DataTableComponent
            controller="ExerciseHistories"
            dataTableDto={historyDto}
            setDataTableDto={setHistoryDto}
            formMode={FormMode.VIEW}
            onButtonClick={() => {}}
            onRowClick={(row) => navigate(`/administrator/workout-plans/${row.workoutPlanId}/view`)}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            dataTableColumns={historyColumns}
            triggerRefreshData={historyRefresh}
            isUrlStateEnabled
            urlStateKey="history"
          />
        </TabPanel>
      </TabView>
    </Card>
  );
}
