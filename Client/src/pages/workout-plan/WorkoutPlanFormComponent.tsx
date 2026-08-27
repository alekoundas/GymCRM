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
import { Checkbox } from "primereact/checkbox";
import { Dropdown } from "primereact/dropdown";

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
      // Put returns the entity, which carries none of the values the api computes
      // (week message, week count, running state). Re-read so they survive a save.
      const reloaded = await apiService.get<WorkoutPlanDto>(
        "WorkoutPlans",
        workoutPlanDto.id
      );
      setWorkoutPlanDto(reloaded ?? response);
      setEditingField(undefined);
      setOriginalValues({});
    }
  };

  return (
    <div className="flex justify-content-between ">
      <div className="w-full"></div>
      <div className="align-items-left w-full">
        <div className="field w-full">
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
            className="w-full"
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

        <div className="field w-full">
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

        <div className="field">
          <label
            htmlFor="isCircular"
            className="block text-900 font-medium mb-2"
          >
            {t("Circular")}
          </label>
          <Checkbox
            checked={workoutPlanDto.isCircular}
            onChange={(e) => updateWorkoutPlanDto({ isCircular: e.checked })}
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT && editingField !== "isCircular")
            }
          />
          {formMode === FormMode.EDIT &&
            isAdminPage &&
            (editingField !== "isCircular" ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit("isCircular")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel("isCircular")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave("isCircular")}
                />
              </>
            ))}
        </div>

        {/*                                       */}
        {/*       Rule (administrator only)       */}
        {/*                                       */}

        {isAdminPage && (
          <div className="field w-full">
            <label
              htmlFor="workoutPlanRuleId"
              className="block text-900 font-medium mb-2"
            >
              {t("Rule")}
            </label>
            <LookupComponent
              controller="WorkoutPlanRules"
              selectedEntityId={workoutPlanDto.workoutPlanRuleId?.toString() ?? ""}
              onChange={(e) =>
                updateWorkoutPlanDto({
                  workoutPlanRuleId: e?.id ? Number(e.id) : undefined,
                })
              }
              isEnabled={
                (formMode === FormMode.EDIT &&
                  editingField === "workoutPlanRuleId") ||
                formMode === FormMode.ADD
              }
            />
            {formMode === FormMode.EDIT &&
              (editingField !== "workoutPlanRuleId" ? (
                <Button
                  icon="pi pi-pencil"
                  className="p-button-rounded p-button-text p-button-secondary"
                  onClick={() => handleEdit("workoutPlanRuleId")}
                  visible={editingField === undefined}
                />
              ) : (
                <>
                  <Button
                    icon="pi pi-times"
                    className="p-button-rounded p-button-text p-button-danger"
                    onClick={() => handleCancel("workoutPlanRuleId")}
                  />
                  <Button
                    icon="pi pi-check"
                    className="p-button-rounded p-button-text p-button-success"
                    onClick={() => handleSave("workoutPlanRuleId")}
                  />
                </>
              ))}
          </div>
        )}

        {/*                                               */}
        {/*       Current week (administrator only)       */}
        {/*                                               */}

        {isAdminPage && (workoutPlanDto.weekCount ?? 0) > 0 && (
          <div className="field w-full">
            <label
              htmlFor="currentWeek"
              className="block text-900 font-medium mb-2"
            >
              {t("Current week")}
            </label>
            <Dropdown
              inputId="currentWeek"
              value={workoutPlanDto.currentWeek ?? null}
              options={Array.from(
                { length: workoutPlanDto.weekCount ?? 0 },
                (_, i) => ({ label: `${t("Week")} ${i + 1}`, value: i + 1 }),
              )}
              onChange={(e) => updateWorkoutPlanDto({ currentWeek: e.value })}
              placeholder={t("Select a value")}
              className="w-full"
              disabled={
                formMode === FormMode.VIEW ||
                (formMode === FormMode.EDIT && editingField !== "currentWeek")
              }
            />
            {formMode === FormMode.EDIT &&
              (editingField !== "currentWeek" ? (
                <Button
                  icon="pi pi-pencil"
                  className="p-button-rounded p-button-text p-button-secondary"
                  onClick={() => handleEdit("currentWeek")}
                  visible={editingField === undefined}
                />
              ) : (
                <>
                  <Button
                    icon="pi pi-times"
                    className="p-button-rounded p-button-text p-button-danger"
                    onClick={() => handleCancel("currentWeek")}
                  />
                  <Button
                    icon="pi pi-check"
                    className="p-button-rounded p-button-text p-button-success"
                    onClick={() => handleSave("currentWeek")}
                  />
                </>
              ))}
          </div>
        )}
      </div>
      <div className="w-full"></div>
    </div>
  );
}
