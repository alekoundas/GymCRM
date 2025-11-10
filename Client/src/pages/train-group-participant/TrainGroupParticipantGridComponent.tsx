import { useState, useEffect, useRef } from "react";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import GenericDialogComponent, {
  DialogChildProps,
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useParams } from "react-router-dom";
import TrainGroupDateParticipantFormComponent from "./TrainGroupDateParticipantFormComponent";
import { TrainGroupParticipantDto } from "../../model/entities/train-group-participant/TrainGroupParticipantDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useDateService } from "../../services/DateService";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import { Tag } from "primereact/tag";

interface IField extends DialogChildProps {
  trainGroupId: number;
  selectedDate: Date;
}

export default function TrainGroupParticipantGridComponent({
  formMode,
  trainGroupId,
  selectedDate,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
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

  const getSelectedDate = () => {
    const dateCleaned = new Date(
      Date.UTC(
        selectedDate.getFullYear(),
        selectedDate.getMonth(),
        selectedDate.getDate(),
        0,
        0,
        0
      )
    );
    console.log("new " + dateCleaned.toISOString());
    return dateCleaned;
  };

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupParticipantDto>
  >({
    ...new DataTableDto(),
    data: trainGroupDto.trainGroupParticipants,
    filters: [
      {
        fieldName: "TrainGroupId",
        value: trainGroupId.toString(),
        filterType: "equals",
      },

      {
        fieldName: "ParticipantGridSelectedDate",
        value: getSelectedDate().toISOString(),
        filterType: "custom",
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
              backgroundColor: "#" + user.userStatus?.color,
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
      sortable: true,
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
      field: "trainGroupId",
      header: "TrainGroupId",
      sortable: true,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "trainGroupDateId",
      header: "TrainGroupDateId",
      sortable: true,
      filter: false,
      filterPlaceholder: t("Search"),
      style: { width: "10%" },
    },
    {
      field: "userId",
      header: t("Participant"),
      sortable: true,
      filter: false,
      filterPlaceholder: t("Search"),
      body: (rowData, options) => chipTemplate(rowData.user),

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
      const response = await apiService.create(
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
      const response = await apiService.update(
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
      const response = await apiService.delete(
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
        formMode={formMode ?? FormMode.VIEW}
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
          <p>{"Are you sure"}?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
