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
      data: [],
      first: 0,
      rows: 10,
      page: 1,
      pageCount: 0,
      filters: [],
      dataTableSorts: [],
    }
  );

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "title",
      header: "Title",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: null,
    },
    {
      field: "startOn",
      header: "Start On",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "duration",
      header: "Duration",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "maxParticipants",
      header: "Max Participants",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "10%" },
      body: null,
    },
    {
      field: "trainerId",
      header: "Trainer",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
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

      {/*                                      */}
      {/*           View Train Group           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <TrainGroupFormComponent />
          <TrainGroupDateGridComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupFormComponent />
          <TrainGroupDateGridComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupFormComponent />
          <TrainGroupDateGridComponent />
        </div>
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
