import { Card } from "primereact/card";
import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
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
import { TrainGroupDto } from "../../model/entities/train-group/TrainGroupDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function TrainGroupAdminPage() {
  const { t } = useTranslator();
  const apiService = useApiService();
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
  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.ADD,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
    ];
  };

  const dataTableColumns: DataTableColumns<TrainGroupDto>[] = [
    {
      field: "title",
      header: t("Title"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "30%" },
    },
    {
      field: "startOn",
      header: t("Start On"),
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData: TrainGroupDto) => {
        if (rowData.startOn) {
          const hours = new Date(rowData.startOn)
            .getUTCHours()
            .toString()
            .padStart(2, "0");
          const minutes = new Date(rowData.startOn)
            .getUTCMinutes()
            .toString()
            .padStart(2, "0");
          return `${hours}:${minutes}`;
        }
      },
      filterPlaceholder: t("Search"),

      style: { width: "20%" },
    },
    {
      field: "duration",
      header: t("Duration"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterTimeComponent options={options} />
      ),
      body: (rowData: TrainGroupDto) => {
        const hours = new Date(rowData.duration)
          .getUTCHours()
          .toString()
          .padStart(2, "0");
        const minutes = new Date(rowData.duration)
          .getUTCMinutes()
          .toString()
          .padStart(2, "0");
        return `${hours}:${minutes}`;
      },
      style: { width: "20%" },
    },
    {
      field: "maxParticipants",
      header: t("Max participants"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterNumberComponent options={options} />
      ),
      style: { width: "10%" },
    },
    {
      field: "trainerId",
      header: t("Trainer"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
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
    const response = await apiService
      .delete("trainGroups", trainGroupDto.id)
      .then(() => {
        dialogControlDelete.hideDialog();
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      });
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
      <Card title={t("Train Groups")}>
        <div className="card">
          <DataTableComponent
            dataTableDto={datatableDto}
            setDataTableDto={setDatatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            controller="TrainGroups"
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={triggerRefreshDataTable}
            availableGridRowButtons={availableGridRowButtons()}
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
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
