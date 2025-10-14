import React, { FormEvent, SyntheticEvent, useEffect, useState } from "react";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { Calendar } from "primereact/calendar";
import { InputTextarea } from "primereact/inputtextarea";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { FormMode } from "../../enum/FormMode";
import { TokenService } from "../../services/TokenService";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";
import { useDateService } from "../../services/DateService";

interface IField extends DialogChildProps {}

export default function TrainGroupFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const { getUTCDate, getUTCTime } = useDateService();
  const { trainGroupDto, updateTrainGroupDto } = useTrainGroupStore();

  // Preset TrainerId
  useEffect(() => {
    if (formMode === FormMode.ADD) {
      const userId = TokenService.getUserId();
      if (userId && !trainGroupDto.trainerId)
        updateTrainGroupDto({ ...trainGroupDto, trainerId: userId });
    }
  }, []);

  const handleChange = (
    e: React.ChangeEvent<any> | { target: { name: string; value: any } }
  ) => {
    const { name, value } = e.target;
    updateTrainGroupDto({ [name]: value });
  };

  return (
    <div className="pr-5 pl-5">
      <div className="field">
        <label
          htmlFor="title"
          className="block text-900 font-medium mb-2"
        >
          {t("Title")}
        </label>
        <InputText
          id="title"
          name="title"
          type="text"
          placeholder={t("Title")}
          className=" mb-4"
          value={trainGroupDto.title}
          onChange={handleChange}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      <div className="field">
        <label
          htmlFor="startOn"
          className="block text-900 font-medium mb-2"
        >
          {t("Starts On")}
        </label>
        <Calendar
          id="startOn"
          name="startOn"
          value={getUTCTime(trainGroupDto.startOn)} // Parse string to Date for display
          onChange={(e) => {
            const inputDate = e.value || new Date("2000-01-01T00:00:00.000Z"); // Fallback to zero
            const selectedHours = inputDate.getHours();
            const selectedMinutes = inputDate.getMinutes();
            const utcIso = new Date(
              Date.UTC(2000, 0, 1, selectedHours, selectedMinutes, 0)
            ).toISOString();
            handleChange({ target: { name: "startOn", value: utcIso } });
          }}
          showIcon
          hourFormat="24"
          placeholder="HH:mm"
          timeOnly
          icon={() => <i className="pi pi-clock" />}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      <div className="field">
        <label
          htmlFor="duration"
          className="block text-900 font-medium mb-2"
        >
          {t("Duration")}
        </label>
        <Calendar
          id="duration"
          name="duration"
          value={getUTCTime(trainGroupDto.duration)} // Parse string to Date
          onChange={(e) => {
            const inputDate = e.value || new Date("2000-01-01T00:00:00.000Z"); // Fallback to zero
            const selectedHours = inputDate.getHours();
            const selectedMinutes = inputDate.getMinutes();
            const utcIso = new Date(
              Date.UTC(2000, 0, 1, selectedHours, selectedMinutes, 0)
            ).toISOString();
            handleChange({ target: { name: "duration", value: utcIso } });
          }}
          showIcon
          hourFormat="24"
          placeholder="HH:mm"
          timeOnly
          icon={() => <i className="pi pi-clock" />}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      <div className="field">
        <label
          htmlFor="maxParticipants"
          className="block text-900 font-medium mb-2"
        >
          {t("Max Participants")}
        </label>
        <InputNumber
          id="maxParticipants"
          name="maxParticipants"
          value={trainGroupDto.maxParticipants}
          onValueChange={(x) =>
            handleChange({
              target: {
                name: "maxParticipants",
                value: x.value ?? 1,
              },
            })
          }
          className=""
          inputClassName="border border-gray-300 rounded p-2"
          showButtons
          min={1}
          max={100}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      <div className="field">
        <label
          htmlFor="trainer"
          className="block text-900 font-medium mb-2"
        >
          {t("Trainer")}
        </label>
        <LookupComponent
          controller="users"
          selectedEntityId={trainGroupDto.trainerId}
          isEnabled={
            formMode !== FormMode.VIEW &&
            TokenService.getRoleName() === "Administrator"
          }
          onChange={(x) =>
            handleChange({
              target: {
                name: "trainerId",
                value: x?.id,
              },
            })
          }
        />
      </div>

      <div className="field">
        <label
          htmlFor="description"
          className="block text-900 font-medium mb-2"
        >
          {t("Description")}
        </label>
        <InputTextarea
          id="description"
          name="description"
          placeholder={t("Description")}
          className="w-full mb-3"
          value={trainGroupDto.description}
          onChange={handleChange}
          disabled={formMode === FormMode.VIEW}
        />
      </div>
    </div>
  );
}
