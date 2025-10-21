import { FormMode } from "../../enum/FormMode";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useEffect } from "react";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Card } from "primereact/card";
import { Button } from "primereact/button";
import WorkoutPlanFormComponent from "./WorkoutPlanFormComponent";
import ExerciseListComponent from "../exercise/ExerciseListComponent";

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

  // Load Initial data
  useEffect(() => {
    if (params["id"]) {
      const id = params["id"];
      apiService.get<WorkoutPlanDto>("WorkoutPlans", id).then((response) => {
        if (response) {
          setWorkoutPlanDto(response);
        }
      });
    }
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
        <div className="card">
          <WorkoutPlanFormComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </div>
      </Card>
      <div className="pt-3">
        <Card
          header={
            <div className="flex justify-content-between align-items-center p-3">
              <div className="flex flex-column gap-1">
                <h2 className="m-0">{t("Exercises")}</h2>
                {/* <p className="m-0 text-gray-600">
                          {t("Select a date to view Participants")}
                        </p> */}
              </div>
              {/* <Button
                        label=""
                        icon="pi pi-info-circle"
                        onClick={() => setInfoDateDialogVisible(true)}
                        className="p-button-text"
                      /> */}
            </div>
          }
        >
          <div className="card">
            <ExerciseListComponent
              formMode={formMode}
              isAdminPage={isAdminPage}
            />
          </div>
        </Card>
      </div>
    </>
  );
}
