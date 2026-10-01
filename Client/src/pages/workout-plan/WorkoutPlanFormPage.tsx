import { FormMode } from "../../enum/FormMode";
import { useApiService } from "../../services/ApiService";
import { useToast } from "../../contexts/ToastContext";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Card } from "primereact/card";
import { Button } from "primereact/button";
import WorkoutPlanFormComponent from "./WorkoutPlanFormComponent";
import WorkoutPlanHeaderComponent, {
  WorkoutPlanHeaderFooter,
} from "./WorkoutPlanHeaderComponent";
import ExerciseListComponent from "../exercise/ExerciseListComponent";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import WorkoutPlanRecordingGridComponent from "../workout-plan-recording/WorkoutPlanRecordingGridComponent";
import WorkoutPlanRecordingActionsComponent from "../workout-plan-recording/WorkoutPlanRecordingActionsComponent";
import { Tag } from "primereact/tag";
import { TokenService } from "../../services/TokenService";
import WorkoutPlanActivationDialogComponent, {
  WorkoutPlanActivationMode,
} from "./WorkoutPlanActivationDialogComponent";

interface IField {
  formMode: FormMode;
}

export default function WorkoutPlanFormPage({ formMode }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { showError } = useToast();
  const params = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isAdminPage = location.pathname.includes("/administrator");
  const { workoutPlanDto, setWorkoutPlanDto } = useWorkoutPlanStore();

  const [isRecordingsDialogVisible, setRecordingsDialogVisibility] =
    useState(false);
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false);
  const [activationMode, setActivationMode] = useState<
    WorkoutPlanActivationMode | undefined
  >(undefined);

  // A member may deactivate their own plan; an admin may also make it active again.
  const canChangeActivation = isAdminPage
    ? TokenService.isUserAllowed("WorkoutPlansAdmin_Edit")
    : TokenService.isUserAllowed("WorkoutPlans_Edit") && !workoutPlanDto.isInactive;
  // Counts the times the plan came back from the server, so anything reading its
  // own data knows to look again.
  const [planVersion, setPlanVersion] = useState(0);

  const dialogControlRecordings: DialogControl = {
    showDialog: () => setRecordingsDialogVisibility(true),
    hideDialog: () => setRecordingsDialogVisibility(false),
  };
  // The dialog edits the plan straight in the store, which is also what the page
  // behind it reads. Cancel has to put back what was there before it opened.
  const planBeforeEdit = useRef<WorkoutPlanDto | null>(null);

  const dialogControlEdit: DialogControl = {
    showDialog: () => {
      planBeforeEdit.current = workoutPlanDto;
      setEditDialogVisibility(true);
    },
    hideDialog: () => {
      if (planBeforeEdit.current) setWorkoutPlanDto(planBeforeEdit.current);
      setEditDialogVisibility(false);
    },
  };

  // A rule with no week would leave the plan pointing at nothing, so it is
  // settled here rather than being sorted out later on the member's Start.
  const isMissingWeek = (): boolean => {
    if (!workoutPlanDto.workoutPlanRuleId || workoutPlanDto.currentWeek)
      return false;

    showError(t("Set the current week for the selected rule"));
    return true;
  };

  const onSaveEdit = async (): Promise<void> => {
    if (isMissingWeek()) return;

    const response = await apiService.update<WorkoutPlanDto>(
      "WorkoutPlans",
      workoutPlanDto,
      workoutPlanDto.id,
    );

    if (response) {
      // Saved, so there is nothing to undo - the reload below brings the truth.
      planBeforeEdit.current = null;
      dialogControlEdit.hideDialog();
      loadWorkoutPlan();
    }
  };

  const loadWorkoutPlan = () => {
    if (params["id"]) {
      const id = params["id"];
      apiService.get<WorkoutPlanDto>("WorkoutPlans", id).then((response) => {
        if (response) {
          setWorkoutPlanDto(response);
          setPlanVersion((previous) => previous + 1);
        }
      });
    }
  };

  // Load Initial data
  useEffect(() => {
    loadWorkoutPlan();
  }, []);

  const onSave = async () => {
    if (formMode === FormMode.ADD) {
      if (isMissingWeek()) return;

      const cleanedChildren: ExerciseDto[] = workoutPlanDto.exercises.map(
        (x: ExerciseDto) => ({
          ...x,
          id: 0,
        })
      );

      const createEntity: WorkoutPlanDto = {
        ...workoutPlanDto,
        exercises: cleanedChildren,
      };

      const response = await apiService.create("WorkoutPlans", createEntity);
      if (response?.[0]) {
        navigate("/administrator/workout-plans");
      }
    }
  };

  // A reading layout, so it gets a column rather than the whole monitor.
  const pageStyle = { maxWidth: "82rem" };

  // Creating a plan is the one case that is a form first and foremost.
  if (formMode === FormMode.ADD) {
    return (
      <div className="mx-auto" style={pageStyle}>
        <Card
          title={t("Workout plan")}
          footer={
            <div className="flex justify-content-end">
              <Button
                label={t("Save")}
                icon="pi pi-check"
                onClick={onSave}
                visible={isAdminPage}
                autoFocus
              />
            </div>
          }
        >
          <WorkoutPlanFormComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </Card>

        <div className="pt-3">
          <ExerciseListComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto" style={pageStyle}>
      {/*                                                            */}
      {/*       Plan on the left, exercises on the right once        */}
      {/*       there is room for two columns                        */}
      {/*                                                            */}

      <div className="grid">
        <div className="col-12 lg:col-4">
          <Card>
            <WorkoutPlanHeaderComponent
              isAdminPage={isAdminPage}
              onEdit={() => dialogControlEdit.showDialog()}
            />

            <WorkoutPlanRecordingActionsComponent
              workoutPlanId={workoutPlanDto.id}
              planVersion={planVersion}
              isAdminPage={isAdminPage}
              onChanged={loadWorkoutPlan}
            />

            <WorkoutPlanHeaderFooter
              onOpenRecordings={() => dialogControlRecordings.showDialog()}
            />

            {canChangeActivation && workoutPlanDto.id > 0 && (
              <div className="flex flex-wrap align-items-center justify-content-between gap-2 pt-3 mt-3 border-top-1 surface-border">
                {workoutPlanDto.isInactive ? (
                  <Tag
                    severity="secondary"
                    icon="pi pi-eye-slash"
                    value={t("Inactive")}
                  />
                ) : (
                  <span />
                )}
                <Button
                  label={workoutPlanDto.isInactive ? t("Activate") : t("Deactivate")}
                  icon={workoutPlanDto.isInactive ? "pi pi-eye" : "pi pi-eye-slash"}
                  severity={workoutPlanDto.isInactive ? "success" : "danger"}
                  size="small"
                  outlined
                  onClick={() =>
                    setActivationMode(workoutPlanDto.isInactive ? "ACTIVATE" : "DEACTIVATE")
                  }
                />
              </div>
            )}
          </Card>
        </div>

        <div className="col-12 lg:col-8">
          <ExerciseListComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </div>
      </div>

      {/*                                    */}
      {/*       Workout plan recordings      */}
      {/*                                    */}

      <GenericDialogComponent
        header={t("Recordings")}
        visible={isRecordingsDialogVisible}
        control={dialogControlRecordings}
        formMode={FormMode.VIEW}
      >
        <WorkoutPlanRecordingGridComponent
          workoutPlanId={workoutPlanDto.id}
          isAdminPage={isAdminPage}
        />
      </GenericDialogComponent>

      <WorkoutPlanActivationDialogComponent
        plan={activationMode ? workoutPlanDto : undefined}
        mode={activationMode ?? "DEACTIVATE"}
        isAdminPage={isAdminPage}
        onHide={() => setActivationMode(undefined)}
        onDone={() => {
          setActivationMode(undefined);
          // A member's deactivated plan is gone from their list, so back to it.
          // Replacing this page means the browser's back button cannot reopen it.
          if (isAdminPage) loadWorkoutPlan();
          else navigate("/workout-plans", { replace: true });
        }}
      />

      {/*                              */}
      {/*       Edit workout plan      */}
      {/*                              */}

      <GenericDialogComponent
        header={t("Edit plan")}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={onSaveEdit}
        formMode={FormMode.EDIT}
      >
        <WorkoutPlanFormComponent
          formMode={FormMode.EDIT}
          isAdminPage={isAdminPage}
        />
      </GenericDialogComponent>
    </div>
  );
}
