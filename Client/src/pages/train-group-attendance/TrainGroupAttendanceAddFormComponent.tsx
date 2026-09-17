import { Calendar } from "primereact/calendar";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";
import { SelectButton } from "primereact/selectbutton";
import { Message } from "primereact/message";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { useTranslator } from "../../services/TranslatorService";
import { TrainGroupAttendanceAddDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceAddDto";

// Either the attendance hangs off a train group, which fills in what the session was,
// or the session is written out by hand - for one whose group no longer exists.
export type AttendanceSource = "GROUP" | "MANUAL";

interface IField extends DialogChildProps {
  source: AttendanceSource;
  setSource: (value: AttendanceSource) => void;
  dto: TrainGroupAttendanceAddDto;
  update: (updates: Partial<TrainGroupAttendanceAddDto>) => void;
}

export default function TrainGroupAttendanceAddFormComponent({
  source,
  setSource,
  dto,
  update,
}: IField) {
  const { t } = useTranslator();

  // A time of day with no date behind it, the way the rest of the app stores one.
  const clockToDate = (value: string | undefined): Date | null => {
    if (!value) return null;
    const date = new Date(value);
    return new Date(2000, 0, 1, date.getUTCHours(), date.getUTCMinutes(), 0);
  };

  const dateToClock = (value: Date | null | undefined): string =>
    value
      ? new Date(
          Date.UTC(2000, 0, 1, value.getHours(), value.getMinutes(), 0)
        ).toISOString()
      : "";

  return (
    <div className="flex flex-column gap-3">
      <div className="field">
        <label className="block text-900 font-medium mb-2">{t("User")} *</label>
        <LookupComponent
          controller="users"
          selectedEntityId={dto.userId}
          isEnabled={true}
          onChange={(e) => update({ userId: e?.id ?? "" })}
        />
      </div>

      <div className="field">
        <label
          htmlFor="attendance-date"
          className="block text-900 font-medium mb-2"
        >
          {t("Attendance date")} *
        </label>
        <Calendar
          inputId="attendance-date"
          value={dto.attendanceDate ? new Date(dto.attendanceDate) : null}
          onChange={(e) =>
            update({
              attendanceDate: e.value ? (e.value as Date).toISOString() : "",
            })
          }
          dateFormat="dd/mm/yy"
          showIcon
          className="w-full"
        />
      </div>

      <div className="field">
        <label className="block text-900 font-medium mb-2">
          {t("Session details")}
        </label>
        <SelectButton
          value={source}
          onChange={(e) => e.value && setSource(e.value)}
          options={[
            { label: t("From a train group"), value: "GROUP" },
            { label: t("Type them in"), value: "MANUAL" },
          ]}
          className="w-full"
        />
      </div>

      {source === "GROUP" ? (
        <div className="field">
          <label className="block text-900 font-medium mb-2">
            {t("Train Group")} *
          </label>
          <LookupComponent
            controller="trainGroups"
            selectedEntityId={dto.trainGroupId?.toString() ?? ""}
            isEnabled={true}
            onChange={(e) => update({ trainGroupId: e?.id ? +e.id : undefined })}
          />
          <small className="text-color-secondary">
            {t("The session will be copied from the group as it is today")}.
          </small>
        </div>
      ) : (
        <>
          <Message
            severity="info"
            className="w-full justify-content-start"
            text={t("Use this for a session whose group no longer exists")}
          />

          <div className="field">
            <label
              htmlFor="attendance-title"
              className="block text-900 font-medium mb-2"
            >
              {t("Group Name")} *
            </label>
            <InputText
              id="attendance-title"
              value={dto.trainGroupTitle}
              onChange={(e) => update({ trainGroupTitle: e.target.value })}
              className="w-full"
            />
          </div>

          <div className="formgrid grid">
            <div className="field col-12 md:col-6">
              <label
                htmlFor="attendance-start"
                className="block text-900 font-medium mb-2"
              >
                {t("Start On")}
              </label>
              <Calendar
                inputId="attendance-start"
                value={clockToDate(dto.trainGroupStartOn)}
                onChange={(e) =>
                  update({ trainGroupStartOn: dateToClock(e.value as Date) })
                }
                timeOnly
                hourFormat="24"
                showIcon
                icon={() => <i className="pi pi-clock" />}
                className="w-full"
              />
            </div>

            <div className="field col-12 md:col-6">
              <label
                htmlFor="attendance-duration"
                className="block text-900 font-medium mb-2"
              >
                {t("Duration")}
              </label>
              <Calendar
                inputId="attendance-duration"
                value={clockToDate(dto.trainGroupDuration)}
                onChange={(e) =>
                  update({ trainGroupDuration: dateToClock(e.value as Date) })
                }
                timeOnly
                hourFormat="24"
                showIcon
                icon={() => <i className="pi pi-clock" />}
                className="w-full"
              />
            </div>
          </div>

          <div className="field">
            <label
              htmlFor="attendance-trainer"
              className="block text-900 font-medium mb-2"
            >
              {t("Trainer")}
            </label>
            <InputText
              id="attendance-trainer"
              value={dto.trainerFullName}
              onChange={(e) => update({ trainerFullName: e.target.value })}
              className="w-full"
            />
          </div>

          <div className="field">
            <label
              htmlFor="attendance-description"
              className="block text-900 font-medium mb-2"
            >
              {t("Description")}
            </label>
            <InputTextarea
              id="attendance-description"
              rows={3}
              value={dto.trainGroupDescription}
              onChange={(e) => update({ trainGroupDescription: e.target.value })}
              className="w-full"
            />
          </div>
        </>
      )}
    </div>
  );
}
