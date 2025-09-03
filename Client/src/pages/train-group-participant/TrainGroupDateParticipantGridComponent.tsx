import { useState, useEffect, useRef } from "react";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { TrainGroupParticipantDto } from "../../model/TrainGroupParticipantDto";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import ApiService from "../../services/ApiService";
import { useParams } from "react-router-dom";
import TrainGroupDateParticipantFormComponent from "./TrainGroupDateParticipantFormComponent";
import { DataTableFilterDto } from "../../model/datatable/DataTableFilterDto";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupDateParticipantGridComponent({
  formMode,
}: IField) {
  const params = useParams();

  const {
    trainGroupDto,
    trainGroupParticipant,
    selectedTrainGroupDate,
    addTrainGroupDateParticipant,
    editTrainGroupDateParticipant,
    resetTrainGroupDateParticipant,
    setTrainGroupParticipant,
    resetTrainGroupParticipant,
  } = useTrainGroupStore();

  const [isViewDialogVisible, setViewDialogVisible] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisible] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisible] = useState(false); // Dialog visibility
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupParticipantDto>) => void) | undefined
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

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupParticipantDto>
  >({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    dataTableSorts: [],
    filters: [
      {
        fieldName: "TrainGroupId",
        value:
          trainGroupDto.id > 0 ? trainGroupDto.id.toString() : params["id"],

        filterType: "equals",
      },
    ],
  });

  // Update datatableDto when trainGroupDto.trainGroupDates changes
  useEffect(() => {
    if (!datatableDto.filters) return;

    let newFilters: DataTableFilterDto[] = datatableDto.filters.filter(
      (x) => x.fieldName !== "TrainGroupDateId"
    );

    let newData = datatableDto.data;
    let newPageCount = datatableDto.pageCount;

    if (selectedTrainGroupDate?.id) {
      newFilters.push({
        fieldName: "TrainGroupDateId",
        value: selectedTrainGroupDate.id.toString(),
        filterType: "equals",
      });

      if (formMode === FormMode.ADD) {
        const participants =
          trainGroupDto.trainGroupDates.find(
            (x) => x.id === selectedTrainGroupDate.id
          )?.trainGroupParticipants ?? [];
        newData = participants;
        newPageCount = participants.length;
      }
    } else {
      newFilters.push({
        fieldName: "TrainGroupDateId",
        value: "0",
        filterType: "equals",
      });

      if (formMode === FormMode.ADD) {
        newData = [];
        newPageCount = 0;
      }
    }

    const newDto: DataTableDto<TrainGroupParticipantDto> = {
      ...datatableDto,
      filters: newFilters,
      data: newData,
      pageCount: newPageCount,
    };

    setDatatableDto(newDto);

    if (formMode !== FormMode.ADD && triggerRefreshDataTable.current) {
      triggerRefreshDataTable.current(newDto);
    }
  }, [selectedTrainGroupDate]);

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "selectedDate",
      header: "Selected Date",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: (rowData: TrainGroupParticipantDto) => {
        if (rowData.selectedDate) {
          const date = new Date(rowData.selectedDate);
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
      field: "trainGroupId",
      header: "TrainGroupId",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "trainGroupDateId",
      header: "TrainGroupDateId",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "userId",
      header: "Participant",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
  ];

  const onAfterDataLoaded = (
    data: DataTableDto<TrainGroupParticipantDto> | null
  ) => {
    if (
      data &&
      trainGroupDto.trainGroupDates.length > 0 &&
      selectedTrainGroupDate
    ) {
      resetTrainGroupDateParticipant(selectedTrainGroupDate.id);
      data.data.forEach((x) => addTrainGroupDateParticipant(x));
      // data.data = [];
    }
    return data;
  };

  const OnSaveAdd = async (): Promise<void> => {
    if (selectedTrainGroupDate?.id) {
      if (formMode === FormMode.ADD) {
        const id =
          (trainGroupDto.trainGroupDates
            .find((x) => x.id === selectedTrainGroupDate.id)
            ?.trainGroupParticipants.filter((x) => x.id < 0).length ?? 0 + 1) *
          -1;

        addTrainGroupDateParticipant({
          id,
          trainGroupId: trainGroupDto.id,
          trainGroupDateId: selectedTrainGroupDate.id,
          userId: trainGroupParticipant.userId, // Use from store (form selection)
          selectedDate: undefined,
        });
        resetTrainGroupParticipant();
        dialogControlAdd.hideDialog();

        // Force DTO refresh to pick up new participant
        const participants =
          trainGroupDto.trainGroupDates.find(
            (x) => x.id === selectedTrainGroupDate.id
          )?.trainGroupParticipants ?? [];
        setDatatableDto((prev) => ({
          ...prev,
          data: participants,
          pageCount: participants.length,
        }));
      } else {
        const response = await ApiService.create(
          "trainGroupParticipants",
          trainGroupParticipant
        );
        if (response) {
          dialogControlAdd.hideDialog();
          resetTrainGroupParticipant();
          if (triggerRefreshDataTable.current)
            triggerRefreshDataTable.current(datatableDto);
        }
      }
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (selectedTrainGroupDate) {
      if (formMode === FormMode.ADD) {
        const trainGroupParticipants = trainGroupDto.trainGroupDates
          .filter((x) => x.id == selectedTrainGroupDate.id)[0]
          .trainGroupParticipants.filter(
            (x) => x.id != trainGroupParticipant.id
          );

        trainGroupParticipant.trainGroupId = trainGroupDto.id;
        trainGroupParticipant.trainGroupDateId = selectedTrainGroupDate.id;
        trainGroupParticipants.push(trainGroupParticipant);

        editTrainGroupDateParticipant(trainGroupParticipant);
        resetTrainGroupParticipant();
        dialogControlEdit.hideDialog();
      } else {
        const response = await ApiService.update(
          "TrainGroupParticipants",
          trainGroupParticipant,
          trainGroupParticipant.id
        );
        if (response) {
          dialogControlEdit.hideDialog();
          resetTrainGroupParticipant();
          if (triggerRefreshDataTable.current)
            triggerRefreshDataTable.current(datatableDto);
        }
      }
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupParticipantDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setTrainGroupParticipant(rowData);
        }
        setViewDialogVisible(true);
        break;
      case ButtonTypeEnum.ADD:
        setAddDialogVisible(true);
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setTrainGroupParticipant(rowData);
        }
        setEditDialogVisible(true);
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
        controller="TrainGroupParticipants"
        formMode={formMode}
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        dataTableColumns={dataTableColumns}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        enableAddAction={
          formMode !== FormMode.VIEW && selectedTrainGroupDate?.id !== undefined
        }
        enableGridRowActions={formMode !== FormMode.VIEW}
        onButtonClick={onDataTableClick}
        onAfterDataLoaded={onAfterDataLoaded}
        triggerRefreshData={triggerRefreshDataTable}
        authorize={true}
        loadDataOnInit={false}
      />

      {/*                                      */}
      {/*           View Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <TrainGroupDateParticipantFormComponent formMode={FormMode.VIEW} />
      </GenericDialogComponent>

      {/*                                      */}
      {/*            Add Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <TrainGroupDateParticipantFormComponent formMode={FormMode.ADD} />
      </GenericDialogComponent>

      {/*                                      */}
      {/*           Edit Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <TrainGroupDateParticipantFormComponent formMode={FormMode.EDIT} />
      </GenericDialogComponent>
    </>
  );
}
