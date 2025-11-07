import { useState, useEffect, useRef } from "react";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useParams } from "react-router-dom";
import { DataTableFilterDto } from "../../model/datatable/DataTableFilterDto";
import TrainGroupDateOneOffParticipantFormComponent from "./TrainGroupDateOneOffParticipantFormComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { Tag } from "primereact/tag";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupDateOneOffParticipantGridComponent({
  formMode,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
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
    filters:
      formMode === FormMode.ADD
        ? []
        : [
            {
              fieldName: "TrainGroupId",
              value:
                trainGroupDto.id > 0
                  ? trainGroupDto.id.toString()
                  : params["id"],

              filterType: "equals",
            },
            {
              fieldName: "SelectedDate",
              value: "null",
              filterType: "notEquals",
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
          <Tag
            className="opacity-100"
            style={{
              backgroundColor: "#" + user.userColor,
            }}
          >
            {" " +
              user.firstName[0].toUpperCase() +
              user.firstName.slice(1, user.firstName.length) +
              " " +
              user.lastName[0].toUpperCase() +
              user.lastName.slice(1, user.lastName.length)}
          </Tag>
        </div>
      );
    }
  };
  const dataTableColumns: DataTableColumns<TrainGroupParticipantDto>[] = [
    {
      field: "selectedDate",
      header: t("Selected Date"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
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
      header: "TrainGroupDateId",
      sortable: false,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "userId",
      header: t("Participant"),
      sortable: formMode !== FormMode.ADD,
      // filter: formMode !== FormMode.ADD,
      filter: false,
      filterPlaceholder: t("Search"),
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

  // Update datatableDto when selected-TrainGroupDate changes
  useEffect(() => {
    if (!datatableDto.filters) return;

    let filters: DataTableFilterDto[] = datatableDto.filters.filter(
      (x) => x.fieldName !== "TrainGroupDateId"
    );

    if (formMode === FormMode.ADD) {
      const newData =
        selectedTrainGroupDate?.trainGroupParticipants.filter(
          (x) => x.selectedDate !== undefined
        ) ?? [];

      setDatatableDto((prev) => ({
        ...prev,
        data: newData,
      }));
    } else {
      filters.push({
        fieldName: "TrainGroupDateId",
        value: selectedTrainGroupDate
          ? selectedTrainGroupDate.id.toString()
          : "0",
        filterType: "equals",
      });
    }

    if (formMode !== FormMode.ADD && triggerRefreshDataTable.current) {
      triggerRefreshDataTable.current({ ...datatableDto, filters: filters });
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
    }
    return data;
  };

  const OnSaveAdd = async (): Promise<void> => {
    if (selectedTrainGroupDate?.id) {
      if (formMode === FormMode.ADD) {
        const id =
          (selectedTrainGroupDate.trainGroupParticipants.length + 1) * -1;

        addTrainGroupDateParticipant({
          id,
          trainGroupId: trainGroupDto.id,
          trainGroupDateId: selectedTrainGroupDate.id,
          userId: trainGroupParticipant.userId,
          selectedDate: trainGroupParticipant.selectedDate,
          user: trainGroupParticipant.user,
        });
        resetTrainGroupParticipant();
        dialogControlAdd.hideDialog();

        // Force DTO refresh to pick up new participant
        setDatatableDto((prev) => ({
          ...prev,
          data: selectedTrainGroupDate.trainGroupParticipants,
        }));
      } else {
        trainGroupParticipant.trainGroupId = trainGroupDto.id;
        trainGroupParticipant.trainGroupDateId = selectedTrainGroupDate.id;
        const response = await apiService.create(
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
        // updateTrainGroupDto(trainGroupDto);

        const participants =
          selectedTrainGroupDate.trainGroupParticipants.filter(
            (x) =>
              x.selectedDate !== undefined && x.id !== trainGroupParticipant.id
          ) ?? [];

        // Force DTO refresh to pick up new participant
        setDatatableDto((prev) => ({
          ...prev,
          data: [...participants, trainGroupParticipant],
          pageCount: [...participants, trainGroupParticipant].length,
        }));

        resetTrainGroupParticipant();
        dialogControlEdit.hideDialog();
      } else {
        const response = await apiService.update(
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
          selectedTrainGroupDate.trainGroupParticipants.filter(
            (x) =>
              x.selectedDate !== undefined && x.id !== trainGroupParticipant.id
          ) ?? [];

        // Force DTO refresh to pick up new participant
        setDatatableDto((prev) => ({
          ...prev,
          data: participants,
        }));

        resetTrainGroupParticipant();
        dialogControlDelete.hideDialog();
      } else {
        const response = await apiService.delete(
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
        resetTrainGroupParticipant();
        setAddDialogVisible(true);
        const participant = new TrainGroupParticipantDto();
        participant.selectedDate = new Date(
          new Date().getFullYear(),
          new Date().getMonth(),
          new Date().getDate(),
          0,
          0,
          0
        ).toISOString();
        setTrainGroupParticipant(participant);
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
        <TrainGroupDateOneOffParticipantFormComponent />
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
        <TrainGroupDateOneOffParticipantFormComponent />
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
        <TrainGroupDateOneOffParticipantFormComponent />
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
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
