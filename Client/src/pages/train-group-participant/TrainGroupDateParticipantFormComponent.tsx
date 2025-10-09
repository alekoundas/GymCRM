import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { InputNumber } from "primereact/inputnumber";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function TrainGroupDateParticipantFormComponent({
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
            htmlFor="trainGroupDateId"
            className="block text-900 font-medium mb-2"
          >
            TrainGroupDateId
          </label>
          <InputNumber
            id="trainGroupDateId"
            name="trainGroupDateId"
            type="text"
            placeholder="TrainGroupDateId"
            className="w-full mb-3"
            value={trainGroupParticipant.trainGroupDateId}
            disabled={true}
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
