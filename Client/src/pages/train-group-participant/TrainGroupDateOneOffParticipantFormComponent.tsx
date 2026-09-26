import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { Calendar } from "primereact/calendar";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";
import { UserDto } from "../../model/entities/user/UserDto";
import { fromUtcDay, toUtcDay } from "../train-group-booking/BookingDates";

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
            // Midnight UTC of the picked day, the way the booking page sends it. A
            // local midnight is the evening before in UTC - in Greece a Friday went to
            // the server as Thursday 21:00, so a Friday group refused it as "not one of
            // the group's days".
            value={
              trainGroupParticipant.selectedDate
                ? fromUtcDay(trainGroupParticipant.selectedDate)
                : undefined
            }
            onChange={(e) => {
              if (e.value) {
                setTrainGroupParticipant({
                  ...trainGroupParticipant,
                  selectedDate: toUtcDay(e.value),
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
                user: x
                  ? ({
                      ...new UserDto(),
                      firstName: x.firstName,
                      lastName: x.lastName,
                      profileImage: x.profileImage,
                    } as UserDto)
                  : undefined,
              })
            }
          />
        </div>
      </div>
    </>
  );
}
