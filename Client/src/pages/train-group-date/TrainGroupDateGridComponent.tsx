import { useState, useEffect, useRef } from "react";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { useParams } from "react-router-dom";
import {
  DataTableSelectionSingleChangeEvent,
  DataTableValueArray,
} from "primereact/datatable";
import GenericDialogComponent, {
  DialogChildProps,
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import TrainGroupDateFormComponent from "./TrainGroupDateFormComponent";
import { TrainGroupDateDto } from "../../model/entities/train-group-date/TrainGroupDateDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function TrainGroupDateGridComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const params = useParams();
  const {
    trainGroupDto,
    trainGroupDateDto,
    selectedTrainGroupDate,
    setSelectedTrainGroupDate,
    addTrainGroupDate,
    setTrainGroupDateDto,
    resetTrainGroupDateDto,

    setTrainGroupDto,
    updateTrainGroupDto,
    resetSelectedTrainGroupDate,
  } = useTrainGroupStore();

  const [isInfoDialogVisible, setInfoDialogVisible] = useState(false); // Dialog visibility
  const [isViewDialogVisible, setViewDialogVisible] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisible] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisible] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupDateDto>) => void) | undefined
  >(undefined);

  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddDialogVisible(true),
    hideDialog: () => setAddDialogVisible(false),
  };
  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisible(true),
    hideDialog: () => setViewDialogVisible(false),
  };
  const dialogControlEdit: DialogControl = {
    showDialog: () => setEditDialogVisible(true),
    hideDialog: () => setEditDialogVisible(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupDateDto>
  >({
    ...new DataTableDto(),
    data: trainGroupDto.trainGroupDates,
    rows: 5,
    filters: [
      {
        fieldName: "TrainGroupId",
        value:
          trainGroupDto.id > 0 ? trainGroupDto.id.toString() : params["id"],
        filterType: "equals",
      },
    ],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    if (formMode !== FormMode.VIEW)
      return [
        ButtonTypeEnum.VIEW,
        ButtonTypeEnum.ADD,
        ButtonTypeEnum.EDIT,
        ButtonTypeEnum.DELETE,
      ];

    return [];
  };

  // Update datatableDto when trainGroupDto.trainGroupDates changes
  useEffect(() => {
    setDatatableDto((prev) => ({
      ...prev,
      data: trainGroupDto.trainGroupDates,
      pageCount: trainGroupDto.trainGroupDates.length,
    }));
  }, [trainGroupDto.trainGroupDates]);

  const dataTableColumns: DataTableColumns<TrainGroupDateDto>[] = [
    {
      field: "id",
      header: "Id",
      sortable: false,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "trainGroupDateType",
      header: t("Type"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
    {
      field: "fixedDay",
      header: t("Fixed Day"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },

      body: (rowData: TrainGroupDateDto) => {
        // return rowData.fixedDay; // Convert Date to string
        if (rowData.fixedDay) {
          const date = new Date(rowData.fixedDay);
          return (
            date.getDate() +
            "/" +
            (date.getMonth() + 1) +
            "/" +
            date.getFullYear()
          );
        }
      },
    },
    {
      field: "recurrenceDayOfWeek",
      header: t("Recurrence Day Of Week"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
      // body: (rowData: TrainGroupDateDto) => rowData.recurrenceDayOfWeek,
    },
    {
      field: "recurrenceDayOfMonth",
      header: t("Recurrence Day Of Month"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "20%" },
    },
  ];

  const onAfterDataLoaded = (data: DataTableDto<TrainGroupDateDto> | null) => {
    if (data) {
      updateTrainGroupDto({ trainGroupDates: data.data });
      data.data = [];
    }
    return data;
  };

  const onSelect = (
    e: DataTableSelectionSingleChangeEvent<DataTableValueArray>
  ) => {
    if (e.value) {
      setSelectedTrainGroupDate(e.value as TrainGroupDateDto);
    } else {
      resetSelectedTrainGroupDate();
    }
  };

  const OnSaveAdd = async (): Promise<void> => {
    trainGroupDateDto.trainGroupId = trainGroupDto.id;
    setTrainGroupDateDto(trainGroupDateDto);

    if (formMode === FormMode.ADD) {
      trainGroupDateDto.id =
        (trainGroupDto.trainGroupDates.filter((x) => x.id < 0).length + 1) * -1;
      addTrainGroupDate(trainGroupDateDto);
      resetTrainGroupDateDto();
      dialogControlAdd.hideDialog();
    } else {
      const response = await apiService.create(
        "TrainGroupDates",
        trainGroupDateDto
      );
      if (response) {
        dialogControlAdd.hideDialog();
        resetTrainGroupDateDto();
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      }
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (formMode === FormMode.ADD) {
      const trainGroupDateDtos = trainGroupDto.trainGroupDates.filter(
        (x) => x.id != trainGroupDateDto.id
      );
      trainGroupDateDto.trainGroupId = trainGroupDto.id;
      trainGroupDateDtos.push(trainGroupDateDto);

      updateTrainGroupDto({ trainGroupDates: trainGroupDateDtos });
      resetTrainGroupDateDto();
      console.log("hideDialog");
      dialogControlEdit.hideDialog();
    } else {
      const response = await apiService.update(
        "TrainGroupDates",
        trainGroupDateDto,
        trainGroupDateDto.id
      );
      if (response) {
        dialogControlEdit.hideDialog();
        resetTrainGroupDateDto();
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      }
    }
  };

  const onDelete = async (): Promise<void> => {
    if (formMode === FormMode.ADD) {
      const trainGroupDateDtos = trainGroupDto.trainGroupDates.filter(
        (x) => x.id != trainGroupDateDto.id
      );

      updateTrainGroupDto({ trainGroupDates: trainGroupDateDtos });
      resetTrainGroupDateDto();
      dialogControlDelete.hideDialog();
    } else {
      const response = await apiService.delete(
        "TrainGroupDates",
        trainGroupDateDto.id
      );
      dialogControlDelete.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDateDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setTrainGroupDateDto(rowData);
          setViewDialogVisible(true);
        }
        break;
      case ButtonTypeEnum.ADD:
        setAddDialogVisible(true);
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setTrainGroupDateDto(rowData);
          setEditDialogVisible(true);
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setTrainGroupDateDto(rowData);
          setDeleteDialogVisibility(true);
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        controller="TrainGroupDates"
        formMode={formMode ?? FormMode.VIEW}
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        dataTableColumns={dataTableColumns}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        onButtonClick={onDataTableClick}
        onAfterDataLoaded={onAfterDataLoaded}
        selectedObject={selectedTrainGroupDate}
        onSelect={onSelect}
        triggerRefreshData={triggerRefreshDataTable}
        authorize={true}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                      */}
      {/*         View TrainGroupDate          */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <TrainGroupDateFormComponent />
      </GenericDialogComponent>

      {/*                                      */}
      {/*          Add TrainGroupDate          */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <TrainGroupDateFormComponent />
      </GenericDialogComponent>

      {/*                                      */}
      {/*         Edit TrainGroupDate          */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <TrainGroupDateFormComponent />
      </GenericDialogComponent>

      {/*                                       */}
      {/*        Delete TrainGroupDate          */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDelete}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
