import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import "./ExerciseListComponent.css"; // Import custom CSS
import { useApiService } from "../../services/ApiService";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
}

export default function ExerciseListComponent({ formMode, isAdminPage }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, setExercises } = useWorkoutPlanStore();

  const updateExercise = (id: number, field: keyof ExerciseDto, value: any) => {
    const updatedExercises = workoutPlanDto.exercises.map((ex) =>
      ex.id === id ? { ...ex, [field]: value } : ex
    );
    setExercises(updatedExercises);
  };

  const addExercise = () => {
    const newExercise = new ExerciseDto();
    newExercise.id = Date.now(); // Temporary unique ID for frontend
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
            label="Add Exercise"
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
                <label htmlFor={`name-${exercise.id}`}>Name</label>
                <InputText
                  id={`name-${exercise.id}`}
                  value={exercise.name}
                  onChange={(e) =>
                    updateExercise(exercise.id, "name", e.target.value)
                  }
                  className="p-inputtext-sm"
                  style={{
                    width: "100%",
                  }}
                />
              </div>
              <div className="field col-12 md:col-3">
                <label htmlFor={`description-${exercise.id}`}>
                  Description
                </label>
                <InputText
                  id={`description-${exercise.id}`}
                  value={exercise.description}
                  onChange={(e) =>
                    updateExercise(exercise.id, "description", e.target.value)
                  }
                  className="p-inputtext-sm"
                  style={{
                    width: "100%",
                  }}
                />
              </div>
              <div className="field col-12 md:col">
                <label htmlFor={`sets-${exercise.id}`}>Sets</label>
                <InputNumber
                  id={`sets-${exercise.id}`}
                  value={exercise.sets}
                  onValueChange={(e) =>
                    updateExercise(exercise.id, "sets", e.value ?? 0)
                  }
                  min={1}
                  showButtons
                  className="p-inputtext-sm w-full"
                  inputStyle={{
                    width: "100%",
                  }}
                />
              </div>
              <div className="field col-12 md:col">
                <label htmlFor={`reps-${exercise.id}`}>Reps</label>
                <InputNumber
                  id={`reps-${exercise.id}`}
                  value={exercise.reps}
                  onValueChange={(e) =>
                    updateExercise(exercise.id, "reps", e.value ?? 0)
                  }
                  min={1}
                  showButtons
                  className="p-inputtext-sm w-full"
                  inputStyle={{
                    width: "100%",
                  }}
                />
              </div>
              <div className="field col-12 md:col">
                <label htmlFor={`weight-${exercise.id}`}>Weight</label>
                <InputNumber
                  id={`weight-${exercise.id}`}
                  value={exercise.weight}
                  onValueChange={(e) =>
                    updateExercise(exercise.id, "weight", e.value ?? 0)
                  }
                  min={0}
                  showButtons
                  className="p-inputtext-sm w-full"
                  inputStyle={{
                    width: "100%",
                  }}
                />
              </div>
              <div className="field col-12 md:col-1">
                <div className="flex justify-content-between align-items-end h-full">
                  <div></div>
                  <Button
                    icon="pi pi-trash"
                    className="p-button-rounded p-button-danger p-button-sm"
                    onClick={() => deleteExercise(exercise.id)}
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
