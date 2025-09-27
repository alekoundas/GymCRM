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
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";

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
    deleteTrainGroupDateParticipant,
    resetTrainGroupParticipant,
    updateTrainGroupDto,
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
    ...new DataTableDto(),
    filters: [
      {
        fieldName: "TrainGroupId",
        value:
          trainGroupDto.id > 0 ? trainGroupDto.id.toString() : params["id"],

        filterType: "equals",
      },
      {
        fieldName: "SelectedDate",
        value: "null",
        filterType: "equals",
      },
      { fieldName: "userId", filterType: "in" },
    ],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    let availableButtons: ButtonTypeEnum[] = [];

    if (formMode !== FormMode.VIEW)
      availableButtons = [
        ButtonTypeEnum.VIEW,
        ButtonTypeEnum.EDIT,
        ButtonTypeEnum.DELETE,
      ];

    if (formMode !== FormMode.VIEW && selectedTrainGroupDate?.id !== undefined)
      availableButtons.push(ButtonTypeEnum.ADD);
    return availableButtons;
  };

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto | undefined) => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = "data:image/png;base64," + user.profileImage;
      return (
        <div className="flex m-0 p-0 align-items-center">
          <Avatar
            image={user.profileImage ? imageSrc : ""}
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="normal"
            className=" mr-2 "
          />
          {" " +
            user.firstName[0].toUpperCase() +
            user.firstName.slice(1, user.firstName.length) +
            " " +
            user.lastName[0].toUpperCase() +
            user.lastName.slice(1, user.lastName.length)}
        </div>
      );
    }
  };

  const dataTableColumns: DataTableColumns<TrainGroupParticipantDto>[] = [
    {
      field: "selectedDate",
      header: "Selected Date",
      sortable: false,
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
      field: "trainGroupDateId",
      header: "Train Group Date Id",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "10%" },
    },
    {
      field: "userId",
      header: "Participant",
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData, options) => chipTemplate(rowData.user),
      style: { width: "10%" },
    },
  ];

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
          trainGroupDto.trainGroupDates
            .find((x) => x.id === selectedTrainGroupDate.id)
            ?.trainGroupParticipants.filter((x) => !x.selectedDate) ?? [];
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
        const participantCount: number =
          trainGroupDto.trainGroupDates
            .find((x) => x.id === selectedTrainGroupDate.id)
            ?.trainGroupParticipants.filter((x) => x.id < 0).length ?? 0;

        const id = (participantCount + 1) * -1;

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
        trainGroupParticipant.trainGroupId = trainGroupDto.id;
        trainGroupParticipant.trainGroupDateId = selectedTrainGroupDate.id;
        const response = await ApiService.create(
          "trainGroupParticipants",
          trainGroupParticipant
        );
        if (response) {
          dialogControlAdd.hideDialog();
          resetTrainGroupParticipant();
          datatableDto.filters.push({
            fieldName: "TrainGroupDateId",
            value: selectedTrainGroupDate.id.toString(),
            filterType: "equals",
          });
          if (triggerRefreshDataTable.current)
            triggerRefreshDataTable.current(datatableDto);
        }
      }
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (selectedTrainGroupDate) {
      if (formMode === FormMode.ADD) {
        editTrainGroupDateParticipant(trainGroupParticipant);

        // Force DTO refresh to pick up new participant
        const participants =
          trainGroupDto.trainGroupDates
            .find((x) => x.id === selectedTrainGroupDate.id)
            ?.trainGroupParticipants.filter(
              (x) =>
                x.selectedDate === undefined &&
                x.id !== trainGroupParticipant.id
            ) ?? [];

        setDatatableDto((prev) => ({
          ...prev,
          data: [...participants, trainGroupParticipant],
          pageCount: [...participants, trainGroupParticipant].length,
        }));

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
          datatableDto.filters.push({
            fieldName: "TrainGroupDateId",
            value: selectedTrainGroupDate.id.toString(),
            filterType: "equals",
          });
          if (triggerRefreshDataTable.current)
            triggerRefreshDataTable.current(datatableDto);
        }
      }
    }
  };

  const onDelete = async (): Promise<void> => {
    if (selectedTrainGroupDate) {
      if (formMode === FormMode.ADD) {
        deleteTrainGroupDateParticipant(trainGroupParticipant);

        const participants =
          trainGroupDto.trainGroupDates
            .find((x) => x.id === selectedTrainGroupDate.id)
            ?.trainGroupParticipants.filter(
              (x) =>
                x.selectedDate !== undefined &&
                x.id !== trainGroupParticipant.id
            ) ?? [];

        // Force DTO refresh to pick up new participant
        setDatatableDto((prev) => ({
          ...prev,
          data: participants,
          pageCount: participants.length,
        }));

        resetTrainGroupParticipant();
        dialogControlDelete.hideDialog();
      } else {
        const response = await ApiService.delete(
          "trainGroupParticipants",
          trainGroupParticipant.id
        );

        dialogControlDelete.hideDialog();
        resetTrainGroupParticipant();

        datatableDto.filters.push({
          fieldName: "TrainGroupDateId",
          value: selectedTrainGroupDate.id.toString(),
          filterType: "equals",
        });
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
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
        formMode={formMode}
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        dataTableColumns={dataTableColumns}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        onButtonClick={onDataTableClick}
        onAfterDataLoaded={onAfterDataLoaded}
        triggerRefreshData={triggerRefreshDataTable}
        authorize={true}
        loadDataOnInit={false}
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
      {/*          Delete Participant           */}
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
