import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useState } from "react";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import ExerciseFormComponent from "./ExerciseFormComponent";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
}

export default function ExerciseListComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, setExercises, newExerciseDto, setNewExerciseDto } =
    useWorkoutPlanStore();

  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddDialogVisibility(true),
    hideDialog: () => setAddDialogVisibility(false),
  };

  const OnDialogAddSave = async (): Promise<void> => {
    newExerciseDto.workoutPlanId = workoutPlanDto.id;
    const response = await apiService.create("Exercises", newExerciseDto);

    if (response && response[0]) {
      dialogControlAdd.hideDialog();
      setExercises([...workoutPlanDto.exercises, response[0]]);
    }
  };

  const addExercise = () => {
    const newExercise = new ExerciseDto();
    newExercise.id = workoutPlanDto.exercises.length * -1;

    if (formMode == FormMode.ADD) {
      setExercises([...workoutPlanDto.exercises, newExercise]);
    } else if (formMode == FormMode.EDIT) {
      setNewExerciseDto(newExercise);
      dialogControlAdd.showDialog();
    }
  };

  return (
    <>
      <div className="w-full">
        <div className="flex justify-content-between align-items-center mb-1">
          <div></div>
          <Button
            label={t("Add Exercise")}
            icon="pi pi-plus"
            onClick={addExercise}
            className="p-button-sm"
          />
        </div>

        <div className="p-2">
          {workoutPlanDto.exercises.map((exercise) => (
            <ExerciseFormComponent
              formMode={formMode}
              exerciseId={exercise.id}
              isAdminPage={isAdminPage}
            />
          ))}
        </div>
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
            exerciseId={newExerciseDto.id}
            isAdminPage={isAdminPage}
          />
        </div>
      </GenericDialogComponent>
    </>
  );
}
