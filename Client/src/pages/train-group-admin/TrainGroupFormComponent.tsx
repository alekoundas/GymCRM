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
import { TrainGroupDto } from "../../model/entities/train-group/TrainGroupDto";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";

interface IField extends DialogChildProps {}

export default function TrainGroupFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { getUTCDate, getUTCTime } = useDateService();
  const { trainGroupDto, updateTrainGroupDto } = useTrainGroupStore();

  const [editingField, setEditingField] = useState<string | undefined>(
    undefined
  ); // Track which field is being edited
  const [originalValues, setOriginalValues] = useState<Partial<TrainGroupDto>>(
    {}
  ); // Store original values per field

  // Preset TrainerId
  useEffect(() => {
    if (formMode === FormMode.ADD) {
      const userId = TokenService.getUserId();
      if (userId && !trainGroupDto.trainerId)
        updateTrainGroupDto({ trainerId: userId });
    }
  }, []);

  // Handle Edit button for a specific field
  const handleEdit = (field: keyof typeof trainGroupDto) => {
    setEditingField(field);
    setOriginalValues({ [field]: trainGroupDto[field] });
  };

  // Handle Cancel for a specific field
  const handleCancel = (field: keyof typeof trainGroupDto) => {
    updateTrainGroupDto({
      [field]: originalValues[field] ?? trainGroupDto[field],
    });
    setEditingField(undefined);
    setOriginalValues({});
  };

  const handleChange = (
    e: React.ChangeEvent<any> | { target: { name: string; value: any } }
  ) => {
    const { name, value } = e.target;
    updateTrainGroupDto({ [name]: value });
  };

  // Handle Save for a specific field
  const handleSave = async (field: keyof typeof trainGroupDto) => {
    const updatedDto = {
      ...trainGroupDto,
      [field]: trainGroupDto[field],
    };
    const response = await apiService.update<TrainGroupDto>(
      "TrainGroups",
      updatedDto,
      trainGroupDto.id
    );
    if (response) {
      updateTrainGroupDto(response); // Update store with backend response
      setEditingField(undefined);
      setOriginalValues({});
    }
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
          value={trainGroupDto.title}
          onChange={handleChange}
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "title")
          }
        />
        {formMode === FormMode.EDIT &&
          (editingField !== "title" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("title")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("title")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("title")}
              />
            </>
          ))}
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
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "startOn")
          }
        />

        {formMode === FormMode.EDIT &&
          (editingField !== "startOn" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("startOn")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("startOn")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("startOn")}
              />
            </>
          ))}
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
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "duration")
          }
        />
        {formMode === FormMode.EDIT &&
          (editingField !== "duration" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("duration")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("duration")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("duration")}
              />
            </>
          ))}
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
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "maxParticipants")
          }
        />
        {formMode === FormMode.EDIT &&
          (editingField !== "maxParticipants" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("maxParticipants")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("maxParticipants")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("maxParticipants")}
              />
            </>
          ))}
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
          // isEnabled={
          //   formMode !== FormMode.VIEW &&
          //   TokenService.getRoleName() === "Administrator"
          // }
          onChange={(x) =>
            handleChange({
              target: {
                name: "trainerId",
                value: x?.id,
              },
            })
          }
          isEnabled={
            ((formMode === FormMode.EDIT && editingField === "trainerId") ||
              formMode === FormMode.ADD) &&
            TokenService.getRoleName() === "Administrator"
          }
        />
        {formMode === FormMode.EDIT &&
          (editingField !== "trainerId" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("trainerId")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("trainerId")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("trainerId")}
              />
            </>
          ))}
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
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "description")
          }
        />
        {formMode === FormMode.EDIT &&
          (editingField !== "description" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("description")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("description")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("description")}
              />
            </>
          ))}
      </div>
    </div>
  );
}
