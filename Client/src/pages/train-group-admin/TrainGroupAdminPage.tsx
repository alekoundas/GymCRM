import { Card } from "primereact/card";
import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { TrainGroupDto } from "../../model/TrainGroupDto";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { useNavigate } from "react-router-dom";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import DataTableFilterTimeComponent from "../../components/core/datatable/DataTableFilterTimeComponent";
import DataTableFilterNumberComponent from "../../components/core/datatable/DataTableFilterNumberComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";

export default function TrainGroupAdminPage() {
  const navigate = useNavigate();
  const {
    trainGroupDto,
    resetTrainGroupDto,
    setTrainGroupDto,
    resetSelectedTrainGroupDate,
    resetTrainGroupParticipant,
  } = useTrainGroupStore();
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupDto>) => void) | undefined
  >(undefined);

  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };
  const [datatableDto, setDatatableDto] = useState<DataTableDto<TrainGroupDto>>(
    {
      ...new DataTableDto(),
      filters: [
        { fieldName: "title", filterType: "contains" },
        { fieldName: "startOn", filterType: "between" },
        { fieldName: "duration", filterType: "between" },
        { fieldName: "maxParticipants", filterType: "equals" },
        { fieldName: "trainerId", filterType: "in" },
      ],
    }
  );

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

  const dataTableColumns: DataTableColumns<TrainGroupDto>[] = [
    {
      field: "title",
      header: "Title",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
    },
    {
      field: "startOn",
      header: "Start On",
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData: TrainGroupDto) => {
        if (rowData.startOn) {
          const date = new Date(rowData.startOn);
          return (
            date.getDate() +
            "/" +
            (date.getMonth() + 1) +
            "/" +
            date.getFullYear()
          );
        }
      },
      filterPlaceholder: "Search",

      style: { width: "20%" },
    },
    {
      field: "duration",
      header: "Duration",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterTimeComponent options={options} />
      ),
      body: (rowData: TrainGroupDto) => {
        if (rowData.duration) {
          const date = new Date(rowData.duration);
          return (
            (date.getHours().toLocaleString().length == 2
              ? date.getHours().toLocaleString()
              : "0" + date.getHours().toLocaleString()) +
            ":" +
            (date.getMinutes().toLocaleString().length == 2
              ? date.getMinutes().toLocaleString()
              : "0" + date.getMinutes().toLocaleString())
          );
        }
      },
      style: { width: "20%" },
    },
    {
      field: "maxParticipants",
      header: "Max Participants",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      filterTemplate: (options) => (
        <DataTableFilterNumberComponent options={options} />
      ),
      style: { width: "10%" },
    },
    {
      field: "trainerId",
      header: "Trainer",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      body: (rowData, options) => chipTemplate(rowData.trainer),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      style: { width: "20%" },
    },
  ];

  const onDelete = async (): Promise<void> => {
    const response = await ApiService.delete("trainGroups", trainGroupDto.id);

    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDto
  ) => {
    resetSelectedTrainGroupDate();
    resetTrainGroupParticipant();
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setTrainGroupDto(rowData);
          navigate(rowData.id + "/view");
        }
        break;
      case ButtonTypeEnum.ADD:
        resetTrainGroupDto();
        navigate("add");
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setTrainGroupDto(rowData);
          navigate(rowData.id + "/edit");
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setTrainGroupDto(rowData);
          dialogControlDelete.showDialog();
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <Card title="Train Groups">
        <div className="card">
          <DataTableComponent
            dataTableDto={datatableDto}
            setDataTableDto={setDatatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            controller="TrainGroups"
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={true}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={triggerRefreshDataTable}
          />
        </div>
      </Card>

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
