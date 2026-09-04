import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";
import { useTrainGroupAttendanceStore } from "../../stores/TrainGroupAttendanceStore";
import { Checkbox } from "primereact/checkbox";
import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

interface IField extends DialogChildProps {
  // participants: TrainGroupParticipantDto[];
}

export default function TrainGroupAttendanceFormComponent({}: IField) {
  const { t } = useTranslator();

  const { selectedUserIds, setSelectedUserIds, trainGroupParticipants } =
    useTrainGroupAttendanceStore();

  // Anyone already marked for this date is not up for selection - the server
  // refuses the duplicate anyway, so offering it would only produce an error.
  const selectableUserIds = trainGroupParticipants
    .filter((x) => !x.hasAttendance)
    .map((x) => x.userId);

  const isAllSelected =
    selectableUserIds.length > 0 &&
    selectableUserIds.every((id) => selectedUserIds.includes(id));

  return (
    <div className="w-full">
      <div className="flex align-items-center gap-2 mb-3">
        <Button
          type="button"
          label={t("Select all")}
          icon="pi pi-check-square"
          className="p-button-sm p-button-outlined"
          disabled={selectableUserIds.length === 0 || isAllSelected}
          onClick={() => setSelectedUserIds(selectableUserIds)}
        />
        {selectedUserIds.length > 0 && (
          <>
            <Button
              type="button"
              label={t("Clear")}
              icon="pi pi-times"
              className="p-button-sm p-button-text p-button-secondary"
              onClick={() => setSelectedUserIds([])}
            />
            <span className="text-sm text-color-secondary">
              {selectedUserIds.length} {t("Selected")}
            </span>
          </>
        )}
      </div>

      {trainGroupParticipants.map((participant) => (
        <div
          key={participant.id}
          className="flex align-items-center gap-2 py-2 border-bottom-1 surface-border"
        >
          <Checkbox
            inputId={`participant-${participant.id}`}
            checked={selectedUserIds.some((id) => id === participant.userId)}
            disabled={participant.hasAttendance}
            onChange={(e) => {
              if (e.checked)
                setSelectedUserIds([...selectedUserIds, participant.userId]);
              else
                setSelectedUserIds(
                  selectedUserIds.filter((id) => id !== participant.userId),
                );
            }}
          />

          <label
            htmlFor={`participant-${participant.id}`}
            className="flex-1"
          >
            {participant.user?.firstName} {participant.user?.lastName}
          </label>

          {participant.hasAttendance && (
            <Tag
              severity="success"
              icon="pi pi-check"
              value={t("Already recorded")}
            />
          )}
        </div>
      ))}
    </div>
  );
}
