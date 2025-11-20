import { useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { ExerciseHistoryDto } from "../../model/entities/exercise-history/ExerciseHistoryDto";
import ExerciseHistoryFormComponent from "./ExerciseHistoryFormComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";

interface IField {
  exerciseId: number;
}

export default function ExerciseHistoryGridComponent({ exerciseId }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { setExerciseHistoryDto } = useWorkoutPlanStore();

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisibility(true),
    hideDialog: () => setViewDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<ExerciseHistoryDto>
  >({
    ...new DataTableDto(),
    filters: [
      {
        fieldName: "ExerciseId",
        value: exerciseId.toString(),
        filterType: "equals",
      },
      {
        fieldName: "name",
        filterType: "contains",
      },
      {
        fieldName: "description",
        filterType: "contains",
      },
      {
        fieldName: "sets",
        filterType: "contains",
      },
      {
        fieldName: "reps",
        filterType: "contains",
      },
      {
        fieldName: "weight",
        filterType: "contains",
      },
      { fieldName: "createdOn", filterType: "between" },
    ],
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [ButtonTypeEnum.VIEW];
  };

  const dataTableColumns: DataTableColumns<ExerciseHistoryDto>[] = [
    {
      field: "name",
      header: t("Name"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "description",
      header: t("Description"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "sets",
      header: t("Sets"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "reps",
      header: t("Reps"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "weight",
      header: t("Weight"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "createdOn",
      header: t("CreatedOn"),
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData, options) => (
        <>
          {new Date(rowData.createdOn).getDate() +
            "/" +
            (new Date(rowData.createdOn).getMonth() + 1) +
            "/" +
            new Date(rowData.createdOn).getFullYear() +
            " " +
            new Date(rowData.createdOn).getHours() +
            ":" +
            new Date(rowData.createdOn).getMinutes()}
        </>
      ),
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
  ];

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: ExerciseHistoryDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setExerciseHistoryDto(rowData);
          dialogControlView.showDialog();
        }
        break;
      case ButtonTypeEnum.ADD:
        break;
      case ButtonTypeEnum.EDIT:
        break;
      case ButtonTypeEnum.DELETE:
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.VIEW}
        onButtonClick={onDataTableClick}
        controller="ExerciseHistories"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                      */}
      {/*           View User status           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <ExerciseHistoryFormComponent />
        </div>
      </GenericDialogComponent>
    </>
  );
}
