import { Card } from "primereact/card";
import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import TrainGroupDateGridComponent from "../train-group-date/TrainGroupDateGridComponent";
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
import { InputNumber } from "primereact/inputnumber";
import DataTableFilterNumberComponent from "../../components/core/datatable/DataTableFilterNumberComponent";

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

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlView: DialogControl = {
    showDialog: () => setViewDialogVisibility(true),
    hideDialog: () => setViewDialogVisibility(false),
  };
  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddDialogVisibility(true),
    hideDialog: () => setAddDialogVisibility(false),
  };
  const dialogControlEdit: DialogControl = {
    showDialog: () => setEditDialogVisibility(true),
    hideDialog: () => setEditDialogVisibility(false),
  };
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
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      style: { width: "20%" },
    },
  ];

  const OnSaveAdd = async (): Promise<void> => {
    const response = await ApiService.create("trainGroups", trainGroupDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetTrainGroupDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    const response = await ApiService.update(
      "trainGroups",
      trainGroupDto,
      trainGroupDto.id
    );

    if (response) {
      dialogControlEdit.hideDialog();
      resetTrainGroupDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

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
