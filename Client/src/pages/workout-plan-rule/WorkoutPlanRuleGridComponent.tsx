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
import { Tag } from "primereact/tag";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";
import { useWorkoutPlanRuleStore } from "../../stores/WorkoutPlanRuleStore";
import {
  DEFAULT_AWAY_GAP_DAYS,
  WorkoutPlanRuleDto,
} from "../../model/entities/workout-plan-rule/WorkoutPlanRuleDto";
import WorkoutPlanRuleFormComponent from "./WorkoutPlanRuleFormComponent";

const CONTROLLER = "WorkoutPlanRules";

export default function WorkoutPlanRuleGridComponent() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const {
    workoutPlanRuleDto,
    setWorkoutPlanRuleDto,
    resetWorkoutPlanRuleDto,
  } = useWorkoutPlanRuleStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanRuleDto>) => void) | undefined
  >(undefined);

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false);
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false);
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false);
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false);

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

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<WorkoutPlanRuleDto>
  >({
    ...new DataTableDto(),
    filters: [{ fieldName: "name", filterType: "contains" as const }],
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    const result: ButtonTypeEnum[] = [];
    if (TokenService.isUserAllowed("WorkoutPlanRules_View"))
      result.push(ButtonTypeEnum.VIEW);
    if (TokenService.isUserAllowed("WorkoutPlanRules_Add"))
      result.push(ButtonTypeEnum.ADD);
    if (TokenService.isUserAllowed("WorkoutPlanRules_Edit"))
      result.push(ButtonTypeEnum.EDIT);
    if (TokenService.isUserAllowed("WorkoutPlanRules_Delete"))
      result.push(ButtonTypeEnum.DELETE);
    return result;
  };

  // weekCount, workoutPlanCount and isLocked are computed server-side rather than
  // stored, so they cannot go through the grid's column-name sorting or filtering.
  const dataTableColumns: DataTableColumns<WorkoutPlanRuleDto>[] = [
    {
      field: "name",
      header: t("Name"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "40%" },
      body: (rowData: WorkoutPlanRuleDto) => (
        <div className="flex align-items-center gap-2 flex-wrap">
          <span>{rowData.name}</span>
          {/* Editing stays open; only Delete waits until the workout ends. */}
          {rowData.isLocked && (
            <Tag
              severity="info"
              icon="pi pi-stopwatch"
              value={t("Workout in progress")}
            />
          )}
        </div>
      ),
    },
    {
      field: "weekCount",
      header: t("Weeks"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "20%" },
    },
    {
      field: "workoutPlanCount",
      header: t("In use"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "25%" },
      body: (rowData: WorkoutPlanRuleDto) =>
        rowData.workoutPlanCount > 0 ? (
          <span>
            {rowData.workoutPlanCount} {t("Workout plans")}
          </span>
        ) : (
          <span className="text-color-secondary">—</span>
        ),
    },
  ];

  const onSaveAdd = async (): Promise<void> => {
    // The form turns Save off while the number of days is empty; this covers a
    // click that lands in the same moment.
    if (workoutPlanRuleDto.awayGapDays == null) return;

    const response = await apiService.create(CONTROLLER, {
      name: workoutPlanRuleDto.name,
      weeks: workoutPlanRuleDto.weeks,
      awayGapDays: workoutPlanRuleDto.awayGapDays,
    });

    if (response) {
      dialogControlAdd.hideDialog();
      resetWorkoutPlanRuleDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    if (!workoutPlanRuleDto.id || workoutPlanRuleDto.awayGapDays == null)
      return;

    const response = await apiService.update(
      CONTROLLER,
      workoutPlanRuleDto,
      workoutPlanRuleDto.id,
    );

    if (response) {
      dialogControlEdit.hideDialog();
      resetWorkoutPlanRuleDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDelete = async (): Promise<void> => {
    if (!workoutPlanRuleDto.id) return;

    await apiService.delete(CONTROLLER, workoutPlanRuleDto.id);
    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  // The grid row carries the weeks already, but re-fetching keeps the
  // "a member is training" note fresh.
  const loadRule = async (rowData: WorkoutPlanRuleDto) => {
    if (!rowData.id) return;
    const response = await apiService.get<WorkoutPlanRuleDto>(
      CONTROLLER,
      rowData.id,
    );
    const rule = response ?? rowData;
    // Older rules have no number of days saved and use the default, so the
    // form shows that number rather than an empty box.
    setWorkoutPlanRuleDto({
      ...rule,
      awayGapDays: rule.awayGapDays ?? DEFAULT_AWAY_GAP_DAYS,
    });
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: WorkoutPlanRuleDto,
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          loadRule(rowData);
          dialogControlView.showDialog();
        }
        break;
      case ButtonTypeEnum.ADD:
        resetWorkoutPlanRuleDto();
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          loadRule(rowData);
          dialogControlEdit.showDialog();
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setWorkoutPlanRuleDto(rowData);
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
        isUrlStateEnabled
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.EDIT}
        onButtonClick={onDataTableClick}
        controller={CONTROLLER}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <WorkoutPlanRuleFormComponent formMode={FormMode.VIEW} />
      </GenericDialogComponent>

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={onSaveAdd}
      >
        <WorkoutPlanRuleFormComponent formMode={FormMode.ADD} />
      </GenericDialogComponent>

      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={onSaveEdit}
      >
        <WorkoutPlanRuleFormComponent formMode={FormMode.EDIT} />
      </GenericDialogComponent>

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
