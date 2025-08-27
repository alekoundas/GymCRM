import React, { FormEvent, SyntheticEvent, useEffect, useState } from "react";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { Calendar } from "primereact/calendar";
import { InputTextarea } from "primereact/inputtextarea";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { FormMode } from "../../enum/FormMode";
import { TokenService } from "../../services/TokenService";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupForm({ formMode }: IField) {
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
          Title
        </label>
        <InputText
          id="title"
          name="title"
          type="text"
          placeholder="Ttitle"
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
          Starts On
        </label>
        <Calendar
          id="startOn"
          name="startOn"
          value={new Date(trainGroupDto.startOn)}
          onChange={(e) => {
            const date = new Date(
              2000,
              0,
              1,
              e.value?.getHours(),
              e.value?.getMinutes(),
              0
            );
            handleChange({ target: { name: "startOn", value: date } });
          }}
          showIcon
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
          Duration
        </label>
        <Calendar
          id="duration"
          name="duration"
          value={new Date(trainGroupDto.duration)}
          onChange={(e) => {
            const date = new Date(
              2000,
              0,
              1,
              e.value?.getHours(),
              e.value?.getMinutes(),
              0
            );
            handleChange({ target: { name: "duration", value: date } });
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
          Max Participants
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
          Trainer
        </label>
        <LookupComponent
          controller="users"
          idValue={trainGroupDto.trainerId}
          isEditable={true}
          isEnabled={
            formMode !== FormMode.VIEW &&
            TokenService.getRoleName() === "Administrator"
          }
          allowCustom={true}
          onChange={(x) =>
            handleChange({
              target: {
                name: "trainerId",
                value: x,
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
          Description
        </label>
        <InputTextarea
          id="description"
          name="description"
          placeholder="Description"
          className="w-full mb-3"
          value={trainGroupDto.description}
          onChange={handleChange}
          disabled={formMode === FormMode.VIEW}
        />
      </div>
    </div>
  );
}
