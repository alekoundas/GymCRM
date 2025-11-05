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
  const { workoutPlanDto, setExercises, newExerciseDto, setNewExerciseDto } =
    useWorkoutPlanStore();

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
            <div></div>
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
    </>
  );
}
