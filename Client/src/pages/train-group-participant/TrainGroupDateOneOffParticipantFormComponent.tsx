import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { Calendar } from "primereact/calendar";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function TrainGroupDateOneOffParticipantFormComponent({
  formMode,
}: IField) {
  const { t } = useTranslator();
  const { trainGroupParticipant, setTrainGroupParticipant } =
    useTrainGroupStore();

  return (
    <>
      <div className="flex justify-content-center p-3">
        <div className="field p-3">
          <label
            htmlFor="selectedDate"
            className="block text-900 font-medium mb-2"
          >
            {t("Selected Date")}
          </label>
          <Calendar
            id="selectedDate"
            name="selectedDate"
            value={
              trainGroupParticipant.selectedDate
                ? new Date(trainGroupParticipant.selectedDate)
                : undefined
            }
            onChange={(e) => {
              if (e.value) {
                const date = new Date(
                  e.value.getFullYear(),
                  e.value.getMonth(),
                  e.value.getDate(),
                  0,
                  0,
                  0
                );

                setTrainGroupParticipant({
                  ...trainGroupParticipant,
                  selectedDate: date.toISOString(),
                });
              }
            }}
            showIcon
            icon={() => <i className="pi pi-clock" />}
            disabled={formMode === FormMode.VIEW}
          />
        </div>

        <div className="field p-3">
          <label
            htmlFor="userId"
            className="block text-900 font-medium mb-2"
          >
            {t("User")}
          </label>
          <LookupComponent
            controller="users"
            selectedEntityId={trainGroupParticipant.userId}
            isEnabled={formMode !== FormMode.VIEW}
            onChange={(x) =>
              setTrainGroupParticipant({
                ...trainGroupParticipant,
                userId: x?.id ?? "",
              })
            }
          />
        </div>
      </div>
    </>
  );
}
