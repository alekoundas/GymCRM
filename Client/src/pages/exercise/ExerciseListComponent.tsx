import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { TokenService } from "../../services/TokenService";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";

interface IField {
  formMode: FormMode;
}

export default function WorkoutPlanGridComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, resetWorkoutPlanDto, setWorkoutPlanDto } =
    useWorkoutPlanStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanDto>) => void) | undefined
  >(undefined);


  return (
    <>
     
    </>
  );
}
