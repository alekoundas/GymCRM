import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import "./ExerciseListComponent.css"; // Import custom CSS
import { useApiService } from "../../services/ApiService";
import { useState } from "react";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
}

export default function ExerciseListComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, setExercises } = useWorkoutPlanStore();

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
    const updatedExercises = workoutPlanDto.exercises.map((ex) =>
      ex.id === id ? { ...ex, [field]: value } : ex
    );
    setExercises(updatedExercises);
  };

  const addExercise = () => {
    const newExercise = new ExerciseDto();
    newExercise.id = workoutPlanDto.exercises.length * -1;
    setExercises([...workoutPlanDto.exercises, newExercise]);
  };

  const deleteExercise = (id: number) => {
    setExercises(workoutPlanDto.exercises.filter((ex) => ex.id !== id));
  };

  return (
    <>
      <div className="w-full">
        <div className="flex justify-content-between align-items-center mb-1">
          <div></div>
          <Button
            label={t("Add Exercise")}
            icon="pi pi-plus"
            onClick={addExercise}
            className="p-button-sm"
          />
        </div>

        <div className="p-2">
          {workoutPlanDto.exercises.map((exercise) => (
            <div
              key={exercise.id}
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
                <label htmlFor={`name-${exercise.id}`}>{t("Name")}</label>
                <div className="w-full flex flex-nowrap">
                  <InputText
                    id={`name-${exercise.id}`}
                    value={exercise.name}
                    onChange={(e) =>
                      updateExercise(exercise.id, "name", e.target.value)
                    }
                    className="p-inputtext-sm w-full"
                    disabled={
                      formMode === FormMode.VIEW ||
                      (formMode === FormMode.EDIT &&
                        (editingField !== "name" ||
                          editingRowId !== exercise.id))
                    }
                  />
                  {formMode === FormMode.EDIT &&
                    ((editingField !== "name" ||
                      editingRowId !== exercise.id) &&
                    isAdminPage ? (
                      <Button
                        icon="pi pi-pencil"
                        className="p-button-rounded p-button-text p-button-secondary"
                        onClick={() => handleEdit(exercise.id, "name")}
                        visible={editingField === undefined}
                      />
                    ) : (
                      <>
                        <Button
                          icon="pi pi-times"
                          className="p-button-rounded p-button-text p-button-danger"
                          onClick={() => handleCancel(exercise.id, "name")}
                        />
                        <Button
                          icon="pi pi-check"
                          className="p-button-rounded p-button-text p-button-success"
                          onClick={() => handleSave(exercise.id, "name")}
                        />
                      </>
                    ))}
                </div>
              </div>

              <div className="field col-12 md:col-3">
                <label htmlFor={`description-${exercise.id}`}>
                  {t("Description")}
                </label>
                <div className="w-full flex flex-nowrap">
                  <InputText
                    id={`description-${exercise.id}`}
                    value={exercise.description}
                    onChange={(e) =>
                      updateExercise(exercise.id, "description", e.target.value)
                    }
                    className="p-inputtext-sm w-full"
                    disabled={
                      formMode === FormMode.VIEW ||
                      (formMode === FormMode.EDIT &&
                        (editingField !== "description" ||
                          editingRowId !== exercise.id))
                    }
                  />
                  {formMode === FormMode.EDIT &&
                    ((editingField !== "description" ||
                      editingRowId !== exercise.id) &&
                    isAdminPage ? (
                      <Button
                        icon="pi pi-pencil"
                        className="p-button-rounded p-button-text p-button-secondary"
                        onClick={() => handleEdit(exercise.id, "description")}
                        visible={editingField === undefined}
                      />
                    ) : (
                      <>
                        <Button
                          icon="pi pi-times"
                          className="p-button-rounded p-button-text p-button-danger"
                          onClick={() =>
                            handleCancel(exercise.id, "description")
                          }
                        />
                        <Button
                          icon="pi pi-check"
                          className="p-button-rounded p-button-text p-button-success"
                          onClick={() => handleSave(exercise.id, "description")}
                        />
                      </>
                    ))}
                </div>
              </div>

              <div className="field col-12 md:col">
                <label htmlFor={`sets-${exercise.id}`}>{t("Sets")}</label>
                <div className="w-full flex flex-nowrap">
                  <InputNumber
                    id={`sets-${exercise.id}`}
                    value={exercise.sets}
                    onValueChange={(e) =>
                      updateExercise(exercise.id, "sets", e.value ?? 0)
                    }
                    min={1}
                    className="p-inputtext-sm w-full"
                    inputStyle={{
                      width: "100%",
                    }}
                    showButtons={
                      formMode !== FormMode.VIEW &&
                      editingField === "sets" &&
                      editingRowId === exercise.id
                    }
                    disabled={
                      formMode === FormMode.VIEW ||
                      (formMode === FormMode.EDIT &&
                        (editingField !== "sets" ||
                          editingRowId !== exercise.id))
                    }
                  />
                  {formMode === FormMode.EDIT &&
                    ((editingField !== "sets" ||
                      editingRowId !== exercise.id) &&
                    isAdminPage ? (
                      <Button
                        icon="pi pi-pencil"
                        className="p-button-rounded p-button-text p-button-secondary"
                        onClick={() => handleEdit(exercise.id, "sets")}
                        visible={editingField === undefined}
                      />
                    ) : (
                      <>
                        <Button
                          icon="pi pi-times"
                          className="p-button-rounded p-button-text p-button-danger"
                          onClick={() => handleCancel(exercise.id, "sets")}
                        />
                        <Button
                          icon="pi pi-check"
                          className="p-button-rounded p-button-text p-button-success"
                          onClick={() => handleSave(exercise.id, "sets")}
                        />
                      </>
                    ))}
                </div>
              </div>

              <div className="field col-12 md:col">
                <label htmlFor={`reps-${exercise.id}`}>{t("Reps")}</label>
                <div className="w-full flex flex-nowrap">
                  <InputNumber
                    id={`reps-${exercise.id}`}
                    value={exercise.reps}
                    onValueChange={(e) =>
                      updateExercise(exercise.id, "reps", e.value ?? 0)
                    }
                    min={1}
                    className="p-inputtext-sm w-full"
                    inputStyle={{
                      width: "100%",
                    }}
                    showButtons={
                      formMode !== FormMode.VIEW &&
                      editingField === "reps" &&
                      editingRowId === exercise.id
                    }
                    disabled={
                      formMode === FormMode.VIEW ||
                      (formMode === FormMode.EDIT &&
                        (editingField !== "reps" ||
                          editingRowId !== exercise.id))
                    }
                  />
                  {formMode === FormMode.EDIT &&
                    ((editingField !== "reps" ||
                      editingRowId !== exercise.id) &&
                    isAdminPage ? (
                      <Button
                        icon="pi pi-pencil"
                        className="p-button-rounded p-button-text p-button-secondary"
                        onClick={() => handleEdit(exercise.id, "reps")}
                        visible={editingField === undefined}
                      />
                    ) : (
                      <>
                        <Button
                          icon="pi pi-times"
                          className="p-button-rounded p-button-text p-button-danger"
                          onClick={() => handleCancel(exercise.id, "reps")}
                        />
                        <Button
                          icon="pi pi-check"
                          className="p-button-rounded p-button-text p-button-success"
                          onClick={() => handleSave(exercise.id, "reps")}
                        />
                      </>
                    ))}
                </div>
              </div>

              <div className="field col-12 md:col">
                <label htmlFor={`weight-${exercise.id}`}>{t("Weight")}</label>
                <div className="w-full flex flex-nowrap">
                  <InputNumber
                    id={`weight-${exercise.id}`}
                    value={exercise.weight}
                    onValueChange={(e) =>
                      updateExercise(exercise.id, "weight", e.value ?? 0)
                    }
                    min={0}
                    showButtons={
                      formMode !== FormMode.VIEW &&
                      editingField === "weight" &&
                      editingRowId === exercise.id
                    }
                    className="p-inputtext-sm w-full"
                    inputStyle={{
                      width: "100%",
                    }}
                    disabled={
                      formMode === FormMode.VIEW ||
                      (formMode === FormMode.EDIT &&
                        (editingField !== "weight" ||
                          editingRowId !== exercise.id))
                    }
                  />
                  {formMode === FormMode.EDIT &&
                    (editingField !== "weight" ||
                    editingRowId !== exercise.id ? (
                      <Button
                        icon="pi pi-pencil"
                        className="p-button-rounded p-button-text p-button-secondary"
                        onClick={() => handleEdit(exercise.id, "weight")}
                        visible={editingField === undefined}
                      />
                    ) : (
                      <>
                        <Button
                          icon="pi pi-times"
                          className="p-button-rounded p-button-text p-button-danger"
                          onClick={() => handleCancel(exercise.id, "weight")}
                        />
                        <Button
                          icon="pi pi-check"
                          className="p-button-rounded p-button-text p-button-success"
                          onClick={() => handleSave(exercise.id, "weight")}
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
                    onClick={() => deleteExercise(exercise.id)}
                    visible={isAdminPage && editingField === undefined}
                  />
                  <div></div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
