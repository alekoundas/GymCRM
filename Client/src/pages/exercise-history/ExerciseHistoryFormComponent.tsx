import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";

interface IField {}

export default function ExerciseHistoryFormComponent({}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { exerciseHistoryDto } = useWorkoutPlanStore();

  return (
    <>
      <div className="field">
        <label htmlFor={`groupNumber-${exerciseHistoryDto.id}`}>
          {t("Group")}
        </label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`groupNumber-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.groupNumber}
            className="p-inputtext-sm w-full"
            disabled={true}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`name-${exerciseHistoryDto.id}`}>{t("Name")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`name-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.name}
            className="p-inputtext-sm w-full"
            disabled={true}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`description-${exerciseHistoryDto.id}`}>
          {t("Description")}
        </label>
        <div className="w-full flex flex-nowrap">
          <InputTextarea
            id={`description-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.description}
            className="p-inputtext-sm w-full"
            disabled={true}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`sets-${exerciseHistoryDto.id}`}>{t("Sets")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`sets-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.sets}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={true}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`reps-${exerciseHistoryDto.id}`}>{t("Reps")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`reps-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.reps}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={true}
          />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`weight-${exerciseHistoryDto.id}`}>{t("Weight")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`weight-${exerciseHistoryDto.id}`}
            value={exerciseHistoryDto.weight}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={true}
          />
        </div>
      </div>
    </>
  );
}
