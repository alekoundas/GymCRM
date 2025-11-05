import { useState } from "react";
import { InputText } from "primereact/inputtext";
import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { InputSwitch } from "primereact/inputswitch";
import { useTranslator } from "../../services/TranslatorService";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { useApiService } from "../../services/ApiService";
import { Button } from "primereact/button";
import LookupComponent from "../../components/core/dropdown/LookupComponent";

interface IField extends DialogChildProps {
  isAdminPage: boolean;
}

export default function WorkoutPlanFormComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, updateWorkoutPlanDto, setWorkoutPlanDto } =
    useWorkoutPlanStore();

  const [editingField, setEditingField] = useState<string | undefined>(
    undefined
  ); // Track which field is being edited
  const [originalValues, setOriginalValues] = useState<Partial<WorkoutPlanDto>>(
    {}
  ); // Store original values per field

  // Handle Edit button for a specific field
  const handleEdit = (field: keyof typeof workoutPlanDto) => {
    setEditingField(field);
    setOriginalValues({ [field]: workoutPlanDto[field] });
  };

  // Handle Cancel for a specific field
  const handleCancel = (field: keyof typeof workoutPlanDto) => {
    updateWorkoutPlanDto({
      [field]: originalValues[field] ?? workoutPlanDto[field],
    });
    setEditingField(undefined);
    setOriginalValues({});
  };

  // Handle Save for a specific field
  const handleSave = async (field: keyof typeof workoutPlanDto) => {
    const updatedDto = {
      ...workoutPlanDto,
      [field]: workoutPlanDto[field],
    };
    const response = await apiService.update<WorkoutPlanDto>(
      "WorkoutPlans",
      updatedDto,
      workoutPlanDto.id
    );
    if (response) {
      setWorkoutPlanDto(response); // Update store with backend response
      setEditingField(undefined);
      setOriginalValues({});
    }
  };

  return (
    <div className="flex flex-column md:flex-row justify-content-center ">
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
          value={workoutPlanDto.title}
          onChange={(e) =>
            updateWorkoutPlanDto({ [e.target.name]: e.target.value })
          }
          disabled={
            formMode === FormMode.VIEW ||
            (formMode === FormMode.EDIT && editingField !== "title")
          }
        />
        {formMode === FormMode.EDIT &&
          isAdminPage &&
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
          htmlFor="userId"
          className="block text-900 font-medium mb-2"
        >
          {t("User")}
        </label>
        <LookupComponent
          controller="users"
          selectedEntityId={workoutPlanDto.userId}
          onChange={(e) => updateWorkoutPlanDto({ userId: e?.id })}
          isEnabled={
            (formMode === FormMode.EDIT && editingField === "userId") ||
            formMode === FormMode.ADD
          }
        />
        {formMode === FormMode.EDIT &&
          isAdminPage &&
          (editingField !== "userId" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("userId")}
              visible={editingField === undefined}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("userId")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("userId")}
              />
            </>
          ))}
      </div>
    </div>
  );
}
