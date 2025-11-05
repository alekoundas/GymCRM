import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { InputTextarea } from "primereact/inputtextarea";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";

interface IField extends DialogChildProps {
  isAdminPage: boolean;
}

export default function ExerciseFormComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();

  const { newExerciseDto, updateNewExerciseDto } = useWorkoutPlanStore();

  const updateExercise = (id: number, field: keyof ExerciseDto, value: any) => {
    updateNewExerciseDto({ ...newExerciseDto, [field]: value });
  };

  return (
    <>
      <div className="field">
        <label htmlFor={`groupNumber-${newExerciseDto.id}`}>{t("Group")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`groupNumber-${newExerciseDto.id}`}
            value={newExerciseDto.groupNumber}
            onValueChange={(e) =>
              updateExercise(newExerciseDto.id, "groupNumber", e.value)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`name-${newExerciseDto.id}`}>{t("Name")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`name-${newExerciseDto.id}`}
            value={newExerciseDto.name}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "name", e.target.value)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`description-${newExerciseDto.id}`}>
          {t("Description")}
        </label>
        <div className="w-full flex flex-nowrap">
          <InputTextarea
            id={`description-${newExerciseDto.id}`}
            value={newExerciseDto.description}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "description", e.target.value)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`sets-${newExerciseDto.id}`}>{t("Sets")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`sets-${newExerciseDto.id}`}
            value={newExerciseDto.sets}
            onValueChange={(e) =>
              updateExercise(newExerciseDto.id, "sets", e.value ?? 0)
            }
            min={1}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`reps-${newExerciseDto.id}`}>{t("Reps")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`reps-${newExerciseDto.id}`}
            value={newExerciseDto.reps}
            onValueChange={(e) =>
              updateExercise(newExerciseDto.id, "reps", e.value ?? 0)
            }
            min={1}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`weight-${newExerciseDto.id}`}>{t("Weight")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`weight-${newExerciseDto.id}`}
            value={newExerciseDto.weight}
            onValueChange={(e) =>
              updateExercise(newExerciseDto.id, "weight", e.value ?? 0)
            }
            min={1}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={formMode === FormMode.VIEW}
          />
        </div>
      </div>
    </>
  );
}
