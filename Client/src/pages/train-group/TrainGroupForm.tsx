import React, { SyntheticEvent, useState } from "react";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { InputSwitch } from "primereact/inputswitch";
import { Button } from "primereact/button";
import { Fieldset } from "primereact/fieldset";
import { Calendar } from "primereact/calendar";
import { Dropdown } from "primereact/dropdown";
import { TrainGroupDto } from "../../model/TrainGroupDto";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { InputTextarea } from "primereact/inputtextarea";
import { FormEvent } from "primereact/ts-helpers";
import LookupComponent from "../../components/dropdown/LookupComponent";
import { FormMode } from "../../enum/FormMode";

interface IField {
  formMode: FormMode;
  // onAfterSave: () => void;
}

export default function TrainGroupForm({ formMode }: IField) {
  const [trainGroupDto, setTrainGroupDto] = useState<TrainGroupDto>({
    id: -1,
    name: "",
    description: "",
    isRepeating: false,
    duration: new Date(),
    startOn: new Date(),
    maxParticipants: 1,
    trainerId: "",
    repeatingParticipants: [],
    trainGroupDates: [],
  });

  // const [newParticipantDto, setNewParticipantDto] =
  //   useState<TrainGroupParticipantDto>({
  //     id: -1,
  //     selectedDate: new Date(),
  //     trainGroupDateId: 0,
  //     participantId: "",
  //   });

  const [trainGroupDateDto, setTrainGroupDateDto] = useState<TrainGroupDateDto>(
    {
      id: -1,
      trainGroupId: 0,
      trainGroupParticipants: [],
      trainGroupCancellationSubscribers: [],
      trainGroup: trainGroupDto,
    }
  );

  const handleChangeTrainGroupDto = (
    event:
      | React.ChangeEvent<any>
      | FormEvent<Date, SyntheticEvent<Element, Event>>
      | { target: { value: any; name?: string } }
  ) => {
    const name = event.target.name;
    const value = event.target.value;

    trainGroupDto[name] = value;
    setTrainGroupDto({ ...trainGroupDto });
  };

  const handleChangeTrainGroupDateDto = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    trainGroupDateDto[name] = value;
    setTrainGroupDateDto({ ...trainGroupDateDto });
  };

  const handleSubmit = async () => {};

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
          onChange={handleChangeTrainGroupDto}
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
          onChange={handleChangeTrainGroupDto}
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
          onChange={handleChangeTrainGroupDto}
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
          onChange={handleChangeTrainGroupDto}
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
            handleChangeTrainGroupDto({
              target: {
                name: "maxParticipants",
                value: x.value,
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
          // onCustomChange={isCustomChange}
          // onCustomSave={handleCustomSave}
          onChange={(x) =>
            handleChangeTrainGroupDto({
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
