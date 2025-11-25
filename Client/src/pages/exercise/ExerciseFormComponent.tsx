import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { InputTextarea } from "primereact/inputtextarea";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { Button } from "primereact/button";
import { useYouTubeService } from "../../services/YouTubeService";
import { Checkbox } from "primereact/checkbox";

interface IField extends DialogChildProps {
  isAdminPage: boolean;
}

export default function ExerciseFormComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const { openYouTubeVideo } = useYouTubeService();

  const { workoutPlanDto, newExerciseDto, updateNewExerciseDto } =
    useWorkoutPlanStore();

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
            // brave + android backspace fix
            mode="decimal"
            useGrouping={false}
            minFractionDigits={0}
            maxFractionDigits={0}
            min={0} // or whatever your minimum is
            allowEmpty={false} // ← now safe to use
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`isCircular-${newExerciseDto.id}`}>
          {t("Circular")}
        </label>
        <div className="w-full flex flex-nowrap">
          <Checkbox
            id={`isCircular-${newExerciseDto.id}`}
            checked={workoutPlanDto.isCircular}
            className="p-inputtext-sm w-full"
            disabled={true}
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
        <label htmlFor={`sets-${newExerciseDto.id}`}>{t("Sets")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`sets-${newExerciseDto.id}`}
            value={newExerciseDto.sets}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "sets", e.target.value ?? 0)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`reps-${newExerciseDto.id}`}>{t("Reps")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`reps-${newExerciseDto.id}`}
            value={newExerciseDto.reps}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "reps", e.target.value ?? 0)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`weight-${newExerciseDto.id}`}>{t("Weight")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`weight-${newExerciseDto.id}`}
            value={newExerciseDto.weight}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "weight", e.target.value ?? 0)
            }
            className="p-inputtext-sm w-full"
            disabled={formMode === FormMode.VIEW}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`videoUrl-${newExerciseDto.id}`}>
          {t("Video Url")}
        </label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`sets-${newExerciseDto.id}`}
            value={newExerciseDto.videoUrl}
            onChange={(e) =>
              updateExercise(newExerciseDto.id, "videoUrl", e.target.value ?? 0)
            }
            className="p-inputtext-sm w-full"
            disabled={!isAdminPage || formMode === FormMode.VIEW}
          />
          <Button
            label="YouTube"
            className="ml-4 w-2"
            icon="pi pi-youtube"
            onClick={() => openYouTubeVideo(newExerciseDto.videoUrl)}
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
            rows={5}
            className="p-inputtext-sm w-full"
            readOnly={!isAdminPage || formMode === FormMode.VIEW}
          />
        </div>
      </div>
    </>
  );
}
