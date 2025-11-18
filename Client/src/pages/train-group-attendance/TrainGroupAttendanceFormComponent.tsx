import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useTrainGroupAttendanceStore } from "../../stores/TrainGroupAttendanceStore";
import { Checkbox } from "primereact/checkbox";

interface IField extends DialogChildProps {
  // participants: TrainGroupParticipantDto[];
}

export default function TrainGroupAttendanceFormComponent({}: IField) {
  const { t } = useTranslator();

  const { selectedUserIds, setSelectedUserIds, trainGroupParticipants } =
    useTrainGroupAttendanceStore();

  return (
    <div className="w-full  ">
      {trainGroupParticipants.map((participant) => (
        <div className="flex justify-content-center">
          <Checkbox
            id={`particiant-${participant.id}`}
            className="w-1"
            checked={selectedUserIds.some((id) => id === participant.userId)}
            key={participant.id}
            onChange={(e) => {
              if (e.checked)
                setSelectedUserIds([...selectedUserIds, participant.userId]);
              else
                setSelectedUserIds(
                  selectedUserIds.filter((id) => id !== participant.userId)
                );
            }}
          />

          <span className="w-full">
            {participant.user?.firstName} - {participant.user?.lastName}
          </span>
        </div>
      ))}
    </div>
  );
}
