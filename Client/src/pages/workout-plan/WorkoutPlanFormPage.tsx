import { FormMode } from "../../enum/FormMode";
import { useApiService } from "../../services/ApiService";
import { useToast } from "../../contexts/ToastContext";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
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

  const dialogControlRecordings: DialogControl = {
    showDialog: () => setRecordingsDialogVisibility(true),
    hideDialog: () => setRecordingsDialogVisibility(false),
  };
  const dialogControlEdit: DialogControl = {
    showDialog: () => setEditDialogVisibility(true),
    hideDialog: () => setEditDialogVisibility(false),
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
              workoutPlanRuleId={workoutPlanDto.workoutPlanRuleId}
              currentWeek={workoutPlanDto.currentWeek}
              isAdminPage={isAdminPage}
              onChanged={loadWorkoutPlan}
            />

            <WorkoutPlanHeaderFooter
              onOpenRecordings={() => dialogControlRecordings.showDialog()}
            />
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
