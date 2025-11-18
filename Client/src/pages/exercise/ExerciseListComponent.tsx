import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useEffect, useState } from "react";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import ExerciseFormComponent from "./ExerciseFormComponent";
import ExerciseListItemComponent from "./ExerciseListItemComponent";
import { Card } from "primereact/card";
import { InputTextarea } from "primereact/inputtextarea";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
}

interface ExerciseGroup {
  groupNumber: number | undefined;
  exercises: ExerciseDto[];
}

export default function ExerciseListComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const {
    workoutPlanDto,
    updateWorkoutPlanDto,
    setExercises,
    newExerciseDto,
    setNewExerciseDto,
    workoutPlanDescription,
    setworkoutPlanDescription,
    setWorkoutPlanDto,
    resetworkoutPlanDescription,
  } = useWorkoutPlanStore();

  const [groups, setGroups] = useState<ExerciseGroup[]>([]);
  const [isWorkoutPlanDialogVisible, setWorkoutPlanDialogVisibility] =
    useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const dialogControlWorkoutPlan: DialogControl = {
    showDialog: () => setWorkoutPlanDialogVisibility(true),
    hideDialog: () => setWorkoutPlanDialogVisibility(false),
  };
  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddDialogVisibility(true),
    hideDialog: () => setAddDialogVisibility(false),
  };

  useEffect(() => {
    setGroups(getGroups());
  }, [workoutPlanDto.exercises]);

  const OnDialogAddSave = async (): Promise<void> => {
    const groupExercisesCount = workoutPlanDto.exercises.filter(
      (x) =>
        x.id !== newExerciseDto.id &&
        x.groupNumber === newExerciseDto.groupNumber
    ).length;
    newExerciseDto.groupExerciseOrderNumber = groupExercisesCount;

    if (formMode === FormMode.ADD) {
      dialogControlAdd.hideDialog();
      setExercises([...workoutPlanDto.exercises, newExerciseDto]);
    } else {
      newExerciseDto.workoutPlanId = workoutPlanDto.id;
      const response = await apiService.create("Exercises", newExerciseDto);

      if (response && response[0]) {
        dialogControlAdd.hideDialog();
        setExercises([...workoutPlanDto.exercises, response[0]]);
      }
    }
  };

  const OnDialogWorkoutPlanSave = async (): Promise<void> => {
    if (formMode === FormMode.ADD) {
      updateWorkoutPlanDto({ description: workoutPlanDescription });
      resetworkoutPlanDescription();
      dialogControlWorkoutPlan.hideDialog();
    }

    if (formMode === FormMode.EDIT) {
      const updatedDto = {
        ...workoutPlanDto,
        description: workoutPlanDescription,
      };
      const response = await apiService.update<WorkoutPlanDto>(
        "WorkoutPlans",
        updatedDto,
        workoutPlanDto.id
      );
      if (response) {
        setWorkoutPlanDto(response); // Update store with backend response
        dialogControlWorkoutPlan.hideDialog();
      }
    }
  };

  const addExercise = () => {
    const newExercise = new ExerciseDto();
    newExercise.id = workoutPlanDto.exercises.length * -1;
    newExercise.groupNumber = workoutPlanDto.exercises.length;

    setNewExerciseDto(newExercise);
    dialogControlAdd.showDialog();
  };

  const getGroups = (): ExerciseGroup[] => {
    const sortedExercises = workoutPlanDto.exercises.sort(
      (x, y) => x.groupNumber - y.groupNumber
    );
    const groups: ExerciseGroup[] = [];

    for (const exercise of sortedExercises) {
      if (groups.some((x) => x.groupNumber === exercise.groupNumber)) {
        groups
          .filter((x) => x.groupNumber === exercise.groupNumber)[0]
          .exercises.push(exercise);
      } else {
        groups.push({
          groupNumber: exercise.groupNumber,
          exercises: [exercise],
        });
      }
    }

    return groups;
  };

  return (
    <>
      <Card>
        <div className="w-full">
          <div className="flex justify-content-between align-items-center">
            <div>
              <Button
                label={t("Update description")}
                icon="pi pi-pencil"
                onClick={() => {
                  setworkoutPlanDescription(workoutPlanDto.description);
                  dialogControlWorkoutPlan.showDialog();
                }}
                className="p-button-sm"
                visible={isAdminPage && formMode !== FormMode.VIEW}
              />
            </div>
            <div></div>
            <div>
              <h2 className="m-0">{t("Exercises")}</h2>
            </div>
            <div></div>
            <div>
              <Button
                label={t("Add Exercise")}
                icon="pi pi-plus"
                onClick={addExercise}
                className="p-button-sm"
                visible={isAdminPage && formMode !== FormMode.VIEW}
              />
            </div>
          </div>

          <div
            className="flex justify-content-center pt-5 break-words " //text-center
            style={{ whiteSpace: "pre-line" }}
          >
            {workoutPlanDto.description}
          </div>
        </div>
      </Card>

      <div className="">
        {groups.map((group) => (
          <Card
            key={group.groupNumber ?? group.exercises[0].id}
            className="mt-3"
          >
            <div className="p-3">
              {group.exercises
                .sort(
                  (x, y) =>
                    x.groupExerciseOrderNumber - y.groupExerciseOrderNumber
                )
                .map((exercise) => (
                  <ExerciseListItemComponent
                    key={exercise.id}
                    formMode={formMode}
                    exerciseId={exercise.id}
                    isAdminPage={isAdminPage}
                  />
                ))}
            </div>
          </Card>
        ))}
      </div>

      {/*                                       */}
      {/*       Add Exercise (Edit Mode)        */}
      {/*                                       */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnDialogAddSave}
      >
        <div className="w-full">
          <ExerciseFormComponent
            formMode={formMode}
            isAdminPage={isAdminPage}
          />
        </div>
      </GenericDialogComponent>

      {/*                                              */}
      {/*       Update Workout Plan Description        */}
      {/*                                              */}

      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isWorkoutPlanDialogVisible}
        control={dialogControlWorkoutPlan}
        onSave={OnDialogWorkoutPlanSave}
      >
        <div className="field">
          <label htmlFor={`description`}>{t("Description")}</label>
          <div className="w-full flex flex-nowrap">
            <InputTextarea
              id={`description`}
              value={workoutPlanDescription}
              onChange={(e) => setworkoutPlanDescription(e.target.value)}
              rows={10}
              className="p-inputtext-sm w-full"
            />
          </div>
        </div>
      </GenericDialogComponent>
    </>
  );
}
