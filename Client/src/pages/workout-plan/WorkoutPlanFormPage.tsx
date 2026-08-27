import { FormMode } from "../../enum/FormMode";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Card } from "primereact/card";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";
import { Message } from "primereact/message";
import WorkoutPlanFormComponent from "./WorkoutPlanFormComponent";
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
  const params = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isAdminPage = location.pathname.includes("/administrator");
  const { workoutPlanDto, setWorkoutPlanDto } = useWorkoutPlanStore();

  const [isRecordingsDialogVisible, setRecordingsDialogVisibility] =
    useState(false);

  const dialogControlRecordings: DialogControl = {
    showDialog: () => setRecordingsDialogVisibility(true),
    hideDialog: () => setRecordingsDialogVisibility(false),
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

  return (
    <>
      <Card
        title={t("Workout plan")}
        footer={
          <div className="flex justify-content-between">
            <div></div>
            <Button
              label={t("Save")}
              icon="pi pi-check"
              onClick={onSave}
              visible={formMode === FormMode.ADD && isAdminPage}
              autoFocus
            />
          </div>
        }
      >
        {/*                                                  */}
        {/*       Current week, message and recordings       */}
        {/*                                                  */}

        {formMode !== FormMode.ADD && (
          <div className="w-full pb-3">
            <div className="flex align-items-center justify-content-between gap-2 flex-wrap">
              <div className="flex align-items-center gap-2">
                {workoutPlanDto.currentWeek ? (
                  <Tag
                    icon="pi pi-replay"
                    value={
                      t("Week") +
                      " " +
                      workoutPlanDto.currentWeek +
                      " " +
                      t("of") +
                      " " +
                      (workoutPlanDto.weekCount ?? 0)
                    }
                  />
                ) : (
                  <Tag
                    severity="secondary"
                    value={
                      workoutPlanDto.workoutPlanRuleId
                        ? t("Never")
                        : t("No rule")
                    }
                  />
                )}
              </div>

              <Button
                label={t("Recordings")}
                icon="pi pi-history"
                className="p-button-sm p-button-outlined"
                onClick={() => dialogControlRecordings.showDialog()}
              />
            </div>

            {(workoutPlanDto.currentWeekMessage?.length ?? 0) > 0 && (
              <Message
                severity="info"
                className="w-full mt-3 justify-content-start"
                text={workoutPlanDto.currentWeekMessage}
              />
            )}

            {workoutPlanDto.hasIncompleteRecording && (
              <Message
                severity="warn"
                className="w-full mt-3 justify-content-start"
                text={
                  t("The recording was left open") +
                  ". " +
                  t("No duration was recorded") +
                  "."
                }
              />
            )}

            <div className="mt-3">
              <WorkoutPlanRecordingActionsComponent
                workoutPlanId={workoutPlanDto.id}
                isAdminPage={isAdminPage}
                onChanged={loadWorkoutPlan}
              />
            </div>
          </div>
        )}

        <div className="card">
          <WorkoutPlanFormComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </div>
      </Card>

      <div className="pt-3">
        <ExerciseListComponent
          formMode={formMode}
          isAdminPage={isAdminPage}
        />
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
    </>
  );
}
