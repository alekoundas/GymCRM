import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { ExerciseDto } from "../../model/entities/exercise/ExerciseDto";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useRef, useState } from "react";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import ExerciseFormComponent from "./ExerciseFormComponent";
import ExerciseHistoryGridComponent from "../exercise-history/ExerciseHistoryGridComponent";

interface IField {
  formMode: FormMode;
  isAdminPage: boolean;
  exerciseId: number;
}

export default function ExerciseListItemComponent({
  formMode,
  isAdminPage,
  exerciseId,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const menuRef = useRef<Menu>(null);

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const [isHistoryDialogVisible, setHistoryDialogVisibility] = useState(false); // Dialog visibility
  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisibility(true),
    hideDialog: () => setViewDialogVisibility(false),
  };
  const dialogControlEdit: DialogControl = {
    showDialog: () => setEditDialogVisibility(true),
    hideDialog: () => setEditDialogVisibility(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };
  const dialogControlHistory: DialogControl = {
    showDialog: () => setHistoryDialogVisibility(true),
    hideDialog: () => setHistoryDialogVisibility(false),
  };

  const { workoutPlanDto, setExercises, newExerciseDto, updateNewExerciseDto } =
    useWorkoutPlanStore();
  const exerciseDto: ExerciseDto = workoutPlanDto.exercises.some(
    (x) => x.id == exerciseId
  )
    ? workoutPlanDto.exercises.find((x) => x.id == exerciseId)!
    : newExerciseDto;

  const onSave = async (rowId: number) => {
    const groupExercisesCount = workoutPlanDto.exercises.filter(
      (x) =>
        x.id !== newExerciseDto.id &&
        x.groupNumber === newExerciseDto.groupNumber
    ).length;

    if (groupExercisesCount > 0) {
      if (newExerciseDto.groupNumber !== exerciseDto.groupNumber) {
        newExerciseDto.groupExerciseOrderNumber = groupExercisesCount;
      }
    } else newExerciseDto.groupExerciseOrderNumber = 0;

    setExercises([
      ...workoutPlanDto.exercises.filter((x) => x.id !== rowId),
      newExerciseDto,
    ]);

    if (formMode === FormMode.EDIT) {
      const response = await apiService.update<ExerciseDto>(
        "Exercises",
        newExerciseDto,
        newExerciseDto.id
      );
      if (response) {
        // setOriginalValues({});
      }
    }

    updateNewExerciseDto(new ExerciseDto());
    dialogControlEdit.hideDialog();
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

  const onMoveUp = async (id: number) => {
    const exercises = [...workoutPlanDto.exercises];
    const current = exercises.find((e) => e.id === id);
    if (!current) return;

    const currGroupNum = current.groupNumber;
    const currGroupExercises = exercises
      .filter((e) => e.groupNumber === currGroupNum)
      .sort((a, b) => a.groupExerciseOrderNumber - b.groupExerciseOrderNumber);
    const currIndexInGroup = currGroupExercises.findIndex((e) => e.id === id);

    if (currIndexInGroup > 0) {
      // Swap order numbers with previous in group
      const prevExercise = currGroupExercises[currIndexInGroup - 1];
      const originalPrevOrder = prevExercise.groupExerciseOrderNumber;
      const originalCurrOrder = current.groupExerciseOrderNumber;

      const newExercises = exercises.map((e) => {
        if (e.id === id) {
          return { ...e, groupExerciseOrderNumber: originalPrevOrder };
        }
        if (e.id === prevExercise.id) {
          return { ...e, groupExerciseOrderNumber: originalCurrOrder };
        }
        return e;
      });

      setExercises(newExercises);

      if (formMode === FormMode.EDIT) {
        await apiService.update<ExerciseDto>(
          "Exercises",
          { ...prevExercise, groupExerciseOrderNumber: originalCurrOrder },
          prevExercise.id
        );
        await apiService.update<ExerciseDto>(
          "Exercises",
          { ...current, groupExerciseOrderNumber: originalPrevOrder },
          id
        );
      }
    } else {
      // First in group, swap group numbers with previous group
      const allGroupNums = [...new Set(exercises.map((e) => e.groupNumber))]
        .filter((gn) => gn !== undefined)
        .sort((a, b) => a - b);
      const currGroupIdx = allGroupNums.indexOf(currGroupNum);
      if (currGroupIdx === 0 || currGroupIdx === -1) return;

      const prevGroupNum = allGroupNums[currGroupIdx - 1];

      const newExercises = exercises.map((e) => {
        if (e.groupNumber === currGroupNum) {
          return { ...e, groupNumber: prevGroupNum };
        } else if (e.groupNumber === prevGroupNum) {
          return { ...e, groupNumber: currGroupNum };
        }
        return e;
      });

      setExercises(newExercises);

      if (formMode === FormMode.EDIT) {
        const prevGroupExercises = exercises.filter(
          (e) => e.groupNumber === prevGroupNum
        );
        const currGroupExercisesFull = exercises.filter(
          (e) => e.groupNumber === currGroupNum
        );

        for (const e of prevGroupExercises) {
          await apiService.update<ExerciseDto>(
            "Exercises",
            { ...e, groupNumber: currGroupNum },
            e.id
          );
        }
        for (const e of currGroupExercisesFull) {
          await apiService.update<ExerciseDto>(
            "Exercises",
            { ...e, groupNumber: prevGroupNum },
            e.id
          );
        }
      }
    }
  };

  const onMoveDown = async (id: number) => {
    const exercises = [...workoutPlanDto.exercises];
    const current = exercises.find((e) => e.id === id);
    if (!current) return;

    const currGroupNum = current.groupNumber;
    const currGroupExercises = exercises
      .filter((e) => e.groupNumber === currGroupNum)
      .sort((a, b) => a.groupExerciseOrderNumber - b.groupExerciseOrderNumber);
    const currIndexInGroup = currGroupExercises.findIndex((e) => e.id === id);

    if (currIndexInGroup < currGroupExercises.length - 1) {
      // Swap order numbers with next in group
      const nextExercise = currGroupExercises[currIndexInGroup + 1];
      const originalNextOrder = nextExercise.groupExerciseOrderNumber;
      const originalCurrOrder = current.groupExerciseOrderNumber;

      const newExercises = exercises.map((e) => {
        if (e.id === id) {
          return { ...e, groupExerciseOrderNumber: originalNextOrder };
        }
        if (e.id === nextExercise.id) {
          return { ...e, groupExerciseOrderNumber: originalCurrOrder };
        }
        return e;
      });

      setExercises(newExercises);

      if (formMode === FormMode.EDIT) {
        await apiService.update<ExerciseDto>(
          "Exercises",
          { ...nextExercise, groupExerciseOrderNumber: originalCurrOrder },
          nextExercise.id
        );
        await apiService.update<ExerciseDto>(
          "Exercises",
          { ...current, groupExerciseOrderNumber: originalNextOrder },
          id
        );
      }
    } else {
      // Last in group, swap group numbers with next group
      const allGroupNums = [...new Set(exercises.map((e) => e.groupNumber))]
        .filter((gn) => gn !== undefined)
        .sort((a, b) => a - b);
      const currGroupIdx = allGroupNums.indexOf(currGroupNum);
      if (currGroupIdx === allGroupNums.length - 1 || currGroupIdx === -1)
        return;

      const nextGroupNum = allGroupNums[currGroupIdx + 1];

      const newExercises = exercises.map((e) => {
        if (e.groupNumber === currGroupNum) {
          return { ...e, groupNumber: nextGroupNum };
        } else if (e.groupNumber === nextGroupNum) {
          return { ...e, groupNumber: currGroupNum };
        }
        return e;
      });

      setExercises(newExercises);

      if (formMode === FormMode.EDIT) {
        const nextGroupExercises = exercises.filter(
          (e) => e.groupNumber === nextGroupNum
        );
        const currGroupExercisesFull = exercises.filter(
          (e) => e.groupNumber === currGroupNum
        );

        for (const e of nextGroupExercises) {
          await apiService.update<ExerciseDto>(
            "Exercises",
            { ...e, groupNumber: currGroupNum },
            e.id
          );
        }
        for (const e of currGroupExercisesFull) {
          await apiService.update<ExerciseDto>(
            "Exercises",
            { ...e, groupNumber: nextGroupNum },
            e.id
          );
        }
      }
    }
  };

  const getCanMoveUp = (id: number): boolean => {
    const exercises = workoutPlanDto.exercises;
    const current = exercises.find((e) => e.id === id);
    if (!current) return false;

    const currGroupNum = current.groupNumber;
    const currGroupExercises = exercises
      .filter((e) => e.groupNumber === currGroupNum)
      .sort((a, b) => a.groupExerciseOrderNumber - b.groupExerciseOrderNumber);
    const currIndexInGroup = currGroupExercises.findIndex((e) => e.id === id);

    if (currIndexInGroup > 0) return true;

    const allGroupNums = [...new Set(exercises.map((e) => e.groupNumber))]
      .filter((gn) => gn !== undefined)
      .sort((a, b) => a - b);
    const currGroupIdx = allGroupNums.indexOf(currGroupNum);
    return currGroupIdx > 0;
  };

  const getCanMoveDown = (id: number): boolean => {
    const exercises = workoutPlanDto.exercises;
    const current = exercises.find((e) => e.id === id);
    if (!current) return false;

    const currGroupNum = current.groupNumber;
    const currGroupExercises = exercises
      .filter((e) => e.groupNumber === currGroupNum)
      .sort((a, b) => a.groupExerciseOrderNumber - b.groupExerciseOrderNumber);
    const currIndexInGroup = currGroupExercises.findIndex((e) => e.id === id);

    if (currIndexInGroup < currGroupExercises.length - 1) return true;

    const allGroupNums = [...new Set(exercises.map((e) => e.groupNumber))]
      .filter((gn) => gn !== undefined)
      .sort((a, b) => a - b);
    const currGroupIdx = allGroupNums.indexOf(currGroupNum);
    return currGroupIdx < allGroupNums.length - 1;
  };

  const getMenuItems: (id: number) => MenuItem[] = (id) => {
    const menuItems: MenuItem[] = [];

    menuItems.push({
      label: t("View"),
      icon: "pi pi-eye",
      command: () => {
        const exercise = workoutPlanDto.exercises.find((x) => x.id === id);
        if (exercise) {
          updateNewExerciseDto({ ...exercise });
        }
        dialogControlView.showDialog();
      },
    });
    menuItems.push({
      label: t("Edit"),
      icon: "pi pi-pencil",
      command: () => {
        const exercise = workoutPlanDto.exercises.find((x) => x.id === id);
        if (exercise) {
          updateNewExerciseDto({ ...exercise });
        }
        dialogControlEdit.showDialog();
      },
      visible: formMode !== FormMode.VIEW,
    });
    menuItems.push({
      label: t("Move up"),
      icon: "pi pi-angle-up",
      command: () => onMoveUp(id),
      disabled: !getCanMoveUp(id),
      visible: isAdminPage && formMode !== FormMode.VIEW,
    });
    menuItems.push({
      label: t("Move down"),
      icon: "pi pi-angle-down",
      command: () => onMoveDown(id),
      disabled: !getCanMoveDown(id),
      visible: isAdminPage && formMode !== FormMode.VIEW,
    });
    menuItems.push({
      label: t("Updates history"),
      icon: "pi pi-history",
      command: () => dialogControlHistory.showDialog(),
      visible: formMode !== FormMode.ADD,
    });
    menuItems.push({
      label: t("Delete"),
      icon: "pi pi-trash",
      command: () => dialogControlDelete.showDialog(),
      visible: isAdminPage && formMode !== FormMode.VIEW,
    });

    return menuItems;
  };

  return (
    <>
      <div
        key={exerciseDto.id}
        className="formgrid grid p-mb-2"
      >
        <div className="field col-12 md:col-4">
          <label htmlFor={`name-${exerciseDto.id}`}>{t("Name")}</label>
          <div className="w-full flex flex-nowrap">
            {/* <InputNumber
              id={`groupNumber-${exerciseDto.id}`}
              value={exerciseDto.groupNumber}
              onValueChange={(e) =>
                updateExercise(exerciseDto.id, "groupNumber", e.value)
              }
              className="p-inputtext-sm w-full"
              disabled={!isAdminPage}
            />
            <InputNumber
              id={`orderNumber-${exerciseDto.id}`}
              value={exerciseDto.groupExerciseOrderNumber}
              onValueChange={(e) =>
                updateExercise(
                  exerciseDto.id,
                  "groupExerciseOrderNumber",
                  e.value
                )
              }
              className="p-inputtext-sm w-full"
              disabled={!isAdminPage}
            /> */}
            <InputText
              id={`name-${exerciseDto.id}`}
              value={exerciseDto.name}
              onChange={(e) =>
                updateExercise(exerciseDto.id, "name", e.target.value)
              }
              className="p-inputtext-sm w-full"
              disabled={true}
            />
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
              disabled={true}
            />
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
              disabled={true}
            />
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
              disabled={true}
            />
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
              min={1}
              className="p-inputtext-sm w-full"
              inputStyle={{
                width: "100%",
              }}
              disabled={true}
            />
          </div>
        </div>

        <div className="field col-12 md:col-1">
          <div className="flex justify-content-between align-items-end h-full">
            <div></div>
            <Button
              icon="pi pi-ellipsis-v"
              className="p-button-rounded p-button-info p-button-sm"
              // onClick={() => dialogControlDelete.showDialog()}
              onClick={(e) => menuRef.current?.toggle(e)}
              // visible={isAdminPage}
            />
            <Menu
              ref={menuRef}
              closeOnEscape
              model={getMenuItems(exerciseDto.id)}
              popup
              appendTo={document.body}
            />
            <div></div>
          </div>
        </div>
      </div>

      {/*                                   */}
      {/*           View Exercise           */}
      {/*                                   */}
      <GenericDialogComponent
        header=""
        visible={isViewDialogVisible}
        control={dialogControlView}
        formMode={FormMode.VIEW}
      >
        <ExerciseFormComponent isAdminPage={isAdminPage} />
      </GenericDialogComponent>

      {/*                                   */}
      {/*           Edit Exercise           */}
      {/*                                   */}
      <GenericDialogComponent
        header=""
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={() => onSave(exerciseDto.id)}
        formMode={FormMode.EDIT}
      >
        <ExerciseFormComponent
          formMode={formMode}
          isAdminPage={isAdminPage}
        />
      </GenericDialogComponent>

      {/*                                    */}
      {/*          Delete Exercise           */}
      {/*                                    */}
      <GenericDialogComponent
        header=""
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={() => deleteExercise(exerciseDto.id)}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>

      {/*                                    */}
      {/*          Exercise History          */}
      {/*                                    */}
      <GenericDialogComponent
        header=""
        visible={isHistoryDialogVisible}
        control={dialogControlHistory}
        formMode={FormMode.VIEW}
      >
        <ExerciseHistoryGridComponent exerciseId={exerciseDto.id} />
      </GenericDialogComponent>
    </>
  );
}
