import React, { SyntheticEvent, useState } from "react";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { Calendar } from "primereact/calendar";
import { InputTextarea } from "primereact/inputtextarea";
import LookupComponent from "../../components/dropdown/LookupComponent";
import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "./TrainGroupStore";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupForm({ formMode }: IField) {
  const { trainGroupDto, updateTrainGroupDto } = useTrainGroupStore();

  const handleChange = (
    e: React.ChangeEvent<any> | { target: { name: string; value: any } }
  ) => {
    const { name, value } = e.target;
    updateTrainGroupDto({ [name]: value });
  };

  return (
    <div className="pr-5 pl-5">
      <h2>Create Training Group</h2>

      <div className="field">
        <label
          htmlFor="name"
          className="block text-900 font-medium mb-2"
        >
          Name
        </label>
        <InputText
          id="name"
          name="name"
          type="text"
          placeholder="Name"
          className="w-full mb-3"
          value={trainGroupDto.name}
          onChange={handleChange}
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
          value={trainGroupDto.startOn}
          onChange={handleChange}
          showIcon
          timeOnly
          icon={() => <i className="pi pi-clock" />}
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
          value={trainGroupDto.duration}
          onChange={handleChange}
          showIcon
          timeOnly
          icon={() => <i className="pi pi-clock" />}
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
          className="w-full"
          inputClassName="border border-gray-300 rounded p-2"
          showButtons
          min={1}
          max={100}
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
          isEnabled={formMode === FormMode.EDIT || formMode === FormMode.ADD}
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
    </div>
  );
}
