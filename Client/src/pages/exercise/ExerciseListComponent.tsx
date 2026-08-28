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
    setExercises,
    newExerciseDto,
    setNewExerciseDto,
  } = useWorkoutPlanStore();

  const [groups, setGroups] = useState<ExerciseGroup[]>([]);
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
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
        x.groupNumber === newExerciseDto.groupNumber,
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

  const addExercise = () => {
    const newExercise = new ExerciseDto();
    newExercise.id = workoutPlanDto.exercises.length * -1;
    newExercise.groupNumber = workoutPlanDto.exercises.length;

    setNewExerciseDto(newExercise);
    dialogControlAdd.showDialog();
  };

  const getGroups = (): ExerciseGroup[] => {
    const sortedExercises = workoutPlanDto.exercises.sort(
      (x, y) => x.groupNumber - y.groupNumber,
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
          <div className="flex flex-wrap align-items-center justify-content-between gap-2">
            <h2 className="m-0">{t("Exercises")}</h2>

            <Button
              label={t("Add Exercise")}
              icon="pi pi-plus"
              onClick={addExercise}
              className="p-button-sm"
              visible={isAdminPage && formMode !== FormMode.VIEW}
            />
          </div>

        </div>
      </Card>

      <div className="grid mt-0">
        {groups.map((group, groupIndex) => {
          const groupExercises = [...group.exercises].sort(
            (x, y) => x.groupExerciseOrderNumber - y.groupExerciseOrderNumber,
          );

          // Exercises are numbered straight through the plan, whether or not
          // they sit in a group - they all have a place in the order.
          const startIndex =
            groups
              .slice(0, groupIndex)
              .reduce((total, x) => total + x.exercises.length, 0) + 1;

          const isGrouped = groupExercises.length > 1;

          return (
            <div
              key={group.groupNumber ?? groupExercises[0].id}
              className={isGrouped ? "col-12 pt-3" : "col-12 xl:col-6 pt-3"}
            >
              <Card className="h-full">
              {isGrouped && (
                <div className="flex align-items-center flex-wrap gap-2 mb-3 pb-2 border-bottom-1 surface-border">
                  <i className="pi pi-link text-primary text-sm" />
                  <span className="text-xs uppercase font-semibold text-primary">
                    {t("Group")} {groupIndex + 1}
                  </span>
                  <span className="text-xs text-color-secondary">
                    · {groupExercises.length} {t("Exercises")}
                  </span>
                </div>
              )}

              <div
                className={
                  isGrouped
                    ? "flex flex-column gap-3 border-left-2 border-primary pl-3"
                    : ""
                }
              >
                {groupExercises.map((exercise, exerciseIndex) => (
                  <div
                    key={exercise.id}
                    className={
                      exerciseIndex > 0
                        ? "pt-3 border-top-1 surface-border"
                        : ""
                    }
                  >
                    <ExerciseListItemComponent
                      formMode={formMode}
                      exerciseId={exercise.id}
                      isAdminPage={isAdminPage}
                      index={startIndex + exerciseIndex}
                    />
                  </div>
                ))}
                </div>
              </Card>
            </div>
          );
        })}
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

    </>
  );
}
