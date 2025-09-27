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

interface IField {
  formMode: FormMode;
}

export default function TrainGroupParticipantGridComponent({
  formMode,
}: IField) {
  const params = useParams();

  const {
    trainGroupDto,
    trainGroupParticipant,
    addTrainGroupParticipant,
    setTrainGroupParticipant,
    updateTrainGroupDto,
    resetTrainGroupParticipant,
  } = useTrainGroupStore();

  const [isViewDialogVisible, setViewDialogVisible] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisible] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisible] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisible] = useState(false); // Dialog visibility
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
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisible(true),
    hideDialog: () => setDeleteDialogVisible(false),
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupParticipantDto>
  >({
    data: trainGroupDto.trainGroupParticipants,
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
      {
        fieldName: "TrainGroupDateId",
        value: "null",
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
      data: trainGroupDto.trainGroupParticipants,
      pageCount: trainGroupDto.trainGroupParticipants.length,
    }));
  }, [trainGroupDto.trainGroupParticipants]);

  const dataTableColumns: DataTableColumns<TrainGroupParticipantDto>[] = [
    {
      field: "selectedDate",
      header: "Selected Date",
      sortable: true,
      filter: false,
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
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "10%" },
    },
    {
      field: "trainGroupDateId",
      header: "TrainGroupDateId",
      sortable: true,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "10%" },
    },
    {
      field: "userId",
      header: "Participant",
      sortable: true,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "10%" },
    },
  ];

  const onAfterDataLoaded = (
    data: DataTableDto<TrainGroupParticipantDto> | null
  ) => {
    if (data) {
      updateTrainGroupDto({ trainGroupParticipants: data.data });
      data.data = [];
    }
    return data;
  };

  const OnSaveAdd = async (): Promise<void> => {
    trainGroupParticipant.trainGroupId = trainGroupDto.id;
    setTrainGroupParticipant(trainGroupParticipant);

    if (formMode === FormMode.ADD) {
      trainGroupParticipant.id =
        (trainGroupDto.trainGroupParticipants.filter((x) => x.id < 0).length +
          1) *
        -1;
      addTrainGroupParticipant(trainGroupParticipant);
      resetTrainGroupParticipant();
      dialogControlAdd.hideDialog();
    } else {
      trainGroupParticipant.trainGroupDateId = undefined;
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
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (formMode === FormMode.ADD) {
      const trainGroupParticipants =
        trainGroupDto.trainGroupParticipants.filter(
          (x) => x.id != trainGroupParticipant.id
        );
      trainGroupParticipant.trainGroupId = trainGroupDto.id;
      trainGroupParticipants.push(trainGroupParticipant);

      updateTrainGroupDto({ trainGroupParticipants: trainGroupParticipants });
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
  };

  const onDelete = async (): Promise<void> => {
    if (formMode === FormMode.ADD) {
      const trainGroupParticipants =
        trainGroupDto.trainGroupParticipants.filter(
          (x) => x.id != trainGroupParticipant.id
        );

      updateTrainGroupDto({ trainGroupParticipants: trainGroupParticipants });
      resetTrainGroupParticipant();
      dialogControlDelete.hideDialog();
    } else {
      const response = await ApiService.delete(
        "TrainGroupParticipants",
        trainGroupParticipant.id
      );

      dialogControlDelete.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
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
          setViewDialogVisible(true);
        }
        break;
      case ButtonTypeEnum.ADD:
        setAddDialogVisible(true);
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setTrainGroupParticipant(rowData);
          setEditDialogVisible(true);
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setTrainGroupParticipant(rowData);
          setDeleteDialogVisible(true);
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        controller="TrainGroupParticipants"
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={formMode}
        dataTableColumns={dataTableColumns}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        onButtonClick={onDataTableClick}
        onAfterDataLoaded={onAfterDataLoaded}
        triggerRefreshData={triggerRefreshDataTable}
        authorize={true}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                      */}
      {/*           View Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <TrainGroupDateParticipantFormComponent />
      </GenericDialogComponent>

      {/*                                      */}
      {/*            Add Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <TrainGroupDateParticipantFormComponent />
      </GenericDialogComponent>

      {/*                                      */}
      {/*           Edit Participant           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <TrainGroupDateParticipantFormComponent />
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Delete Train Group           */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDelete}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>Are you sure?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
