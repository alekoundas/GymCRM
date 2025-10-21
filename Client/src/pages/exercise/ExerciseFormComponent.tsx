import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useState } from "react";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
  exerciseId: number;
}

export default function ExerciseFormComponent({
  formMode,
  isAdminPage,
  exerciseId,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const { workoutPlanDto, setExercises, newExerciseDto, updateNewExerciseDto } =
    useWorkoutPlanStore();
  const exerciseDto: ExerciseDto = workoutPlanDto.exercises.some(
    (x) => x.id == exerciseId
  )
    ? workoutPlanDto.exercises.find((x) => x.id == exerciseId)!
    : newExerciseDto;

  const [editingField, setEditingField] = useState<string | undefined>(
    undefined
  ); // Track which field is being edited
  const [editingRowId, setEditingRowId] = useState<number | undefined>(
    undefined
  ); // Track which field is being edited

  const [originalValues, setOriginalValues] = useState<Partial<ExerciseDto>>(
    {}
  ); // Store original values per field

  // Handle Edit button for a specific field
  const handleEdit = (rowId: number, field: keyof ExerciseDto) => {
    const exersice = workoutPlanDto.exercises.find((x) => x.id == rowId);
    if (exersice) {
      setEditingField(field);
      setEditingRowId(rowId);
      setOriginalValues({ [field]: exersice[field] });
    }
  };

  // Handle Cancel for a specific field
  const handleCancel = (rowId: number, field: keyof ExerciseDto) => {
    const exersice = workoutPlanDto.exercises.find((x) => x.id == rowId);
    if (exersice) {
      const originalValue = originalValues[field] ?? exersice[field];
      updateExercise(rowId, field, originalValue);
      setEditingField(undefined);
      setEditingRowId(undefined);
      setOriginalValues({});
    }
  };

  // Handle Save for a specific field
  const handleSave = async (rowId: number, field: keyof ExerciseDto) => {
    const exersice = workoutPlanDto.exercises.find((x) => x.id == rowId);
    if (exersice) {
      const updatedDto = {
        ...exersice,
        [field]: exersice[field],
      };
      const response = await apiService.update<ExerciseDto>(
        "Exercises",
        updatedDto,
        updatedDto.id
      );
      if (response) {
        const updatedExercises = workoutPlanDto.exercises.filter(
          (ex) => ex.id !== rowId
        );
        updatedExercises.push(response);
        setExercises(updatedExercises);
        setEditingField(undefined);
        setEditingRowId(undefined);
        setOriginalValues({});
      }
    }
  };

  const updateExercise = (id: number, field: keyof ExerciseDto, value: any) => {
    if (workoutPlanDto.exercises.some((x) => x.id === id)) {
      const updatedExercises = workoutPlanDto.exercises.map((x) =>
        x.id === id ? { ...x, [field]: value } : x
      );
      setExercises(updatedExercises);
    } else {
      updateNewExerciseDto({ [field]: value });
    }
  };

  const deleteExercise = async (id: number): Promise<void> => {
    setExercises(workoutPlanDto.exercises.filter((ex) => ex.id !== id));

    if (formMode === FormMode.EDIT) {
      const response = await apiService.delete("Exercises", exerciseDto.id);
    }
    dialogControlDelete.hideDialog();
  };

  return (
    <div
      key={exerciseDto.id}
      className="formgrid grid p-mb-2"
      style={
        {
          // padding: "1rem",
          // backgroundColor: "#f8f9fa",
          // borderRadius: "8px",
          // boxShadow: "0 2px 4px rgba(0, 0, 0, 0.1)",
          // border: "1px solid #e0e0e0",
        }
      }
    >
      <div className="field col-12 md:col-4">
        <label htmlFor={`name-${exerciseDto.id}`}>{t("Name")}</label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`name-${exerciseDto.id}`}
            value={exerciseDto.name}
            onChange={(e) =>
              updateExercise(exerciseDto.id, "name", e.target.value)
            }
            className="p-inputtext-sm w-full"
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT &&
                (editingField !== "name" || editingRowId !== exerciseDto.id))
            }
          />
          {formMode === FormMode.EDIT &&
            ((editingField !== "name" || editingRowId !== exerciseDto.id) &&
            isAdminPage ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit(exerciseDto.id, "name")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel(exerciseDto.id, "name")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave(exerciseDto.id, "name")}
                />
              </>
            ))}
        </div>
      </div>

      <div className="field col-12 md:col-3">
        <label htmlFor={`description-${exerciseDto.id}`}>
          {t("Description")}
        </label>
        <div className="w-full flex flex-nowrap">
          <InputText
            id={`description-${exerciseDto.id}`}
            value={exerciseDto.description}
            onChange={(e) =>
              updateExercise(exerciseDto.id, "description", e.target.value)
            }
            className="p-inputtext-sm w-full"
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT &&
                (editingField !== "description" ||
                  editingRowId !== exerciseDto.id))
            }
          />
          {formMode === FormMode.EDIT &&
            ((editingField !== "description" ||
              editingRowId !== exerciseDto.id) &&
            isAdminPage ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit(exerciseDto.id, "description")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel(exerciseDto.id, "description")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave(exerciseDto.id, "description")}
                />
              </>
            ))}
        </div>
      </div>

      <div className="field col-12 md:col">
        <label htmlFor={`sets-${exerciseDto.id}`}>{t("Sets")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`sets-${exerciseDto.id}`}
            value={exerciseDto.sets}
            onValueChange={(e) =>
              updateExercise(exerciseDto.id, "sets", e.value ?? 0)
            }
            min={1}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            showButtons={
              formMode !== FormMode.VIEW &&
              editingField === "sets" &&
              editingRowId === exerciseDto.id
            }
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT &&
                (editingField !== "sets" || editingRowId !== exerciseDto.id))
            }
          />
          {formMode === FormMode.EDIT &&
            ((editingField !== "sets" || editingRowId !== exerciseDto.id) &&
            isAdminPage ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit(exerciseDto.id, "sets")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel(exerciseDto.id, "sets")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave(exerciseDto.id, "sets")}
                />
              </>
            ))}
        </div>
      </div>

      <div className="field col-12 md:col">
        <label htmlFor={`reps-${exerciseDto.id}`}>{t("Reps")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`reps-${exerciseDto.id}`}
            value={exerciseDto.reps}
            onValueChange={(e) =>
              updateExercise(exerciseDto.id, "reps", e.value ?? 0)
            }
            min={1}
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            showButtons={
              formMode !== FormMode.VIEW &&
              editingField === "reps" &&
              editingRowId === exerciseDto.id
            }
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT &&
                (editingField !== "reps" || editingRowId !== exerciseDto.id))
            }
          />
          {formMode === FormMode.EDIT &&
            ((editingField !== "reps" || editingRowId !== exerciseDto.id) &&
            isAdminPage ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit(exerciseDto.id, "reps")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel(exerciseDto.id, "reps")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave(exerciseDto.id, "reps")}
                />
              </>
            ))}
        </div>
      </div>

      <div className="field col-12 md:col">
        <label htmlFor={`weight-${exerciseDto.id}`}>{t("Weight")}</label>
        <div className="w-full flex flex-nowrap">
          <InputNumber
            id={`weight-${exerciseDto.id}`}
            value={exerciseDto.weight}
            onValueChange={(e) =>
              updateExercise(exerciseDto.id, "weight", e.value ?? 0)
            }
            min={0}
            showButtons={
              formMode !== FormMode.VIEW &&
              editingField === "weight" &&
              editingRowId === exerciseDto.id
            }
            className="p-inputtext-sm w-full"
            inputStyle={{
              width: "100%",
            }}
            disabled={
              formMode === FormMode.VIEW ||
              (formMode === FormMode.EDIT &&
                (editingField !== "weight" || editingRowId !== exerciseDto.id))
            }
          />
          {formMode === FormMode.EDIT &&
            (editingField !== "weight" || editingRowId !== exerciseDto.id ? (
              <Button
                icon="pi pi-pencil"
                className="p-button-rounded p-button-text p-button-secondary"
                onClick={() => handleEdit(exerciseDto.id, "weight")}
                visible={editingField === undefined}
              />
            ) : (
              <>
                <Button
                  icon="pi pi-times"
                  className="p-button-rounded p-button-text p-button-danger"
                  onClick={() => handleCancel(exerciseDto.id, "weight")}
                />
                <Button
                  icon="pi pi-check"
                  className="p-button-rounded p-button-text p-button-success"
                  onClick={() => handleSave(exerciseDto.id, "weight")}
                />
              </>
            ))}
        </div>
      </div>

      <div className="field col-12 md:col-1">
        <div className="flex justify-content-between align-items-end h-full">
          <div></div>
          <Button
            icon="pi pi-trash"
            className="p-button-rounded p-button-danger p-button-sm"
            onClick={() => dialogControlDelete.showDialog()}
            visible={isAdminPage && editingField === undefined}
          />
          <div></div>
        </div>
      </div>

      {/*                                       */}
      {/*          Delete User status           */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={() => deleteExercise(exerciseDto.id)}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>
    </div>
  );
}
