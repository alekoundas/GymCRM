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

export default function TrainGroupAdminPage() {
  const { trainGroupDto, resetTrainGroupDto, setTrainGroupDto } =
    useTrainGroupStore();
  const onRefreshDataTable = useRef<(() => void) | undefined>(undefined);

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility

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

  const datatableDto: DataTableDto<TrainGroupDto> = {
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    dataTableSorts: [],
    dataTableFilters: {},
  };

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
      if (onRefreshDataTable.current) onRefreshDataTable.current();
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
      if (onRefreshDataTable.current) onRefreshDataTable.current();
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDto
  ) => {
    if (rowData) setTrainGroupDto({ ...rowData });
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        dialogControlView.showDialog();
        break;
      case ButtonTypeEnum.ADD:
        resetTrainGroupDto();
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        dialogControlEdit.showDialog();
        break;
      case ButtonTypeEnum.DELETE:
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
            dataTable={datatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            controller="TrainGroups"
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={true}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={onRefreshDataTable}
          />
        </div>
      </Card>

      {/*                                      */}
      {/*           View Train Group           */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.VIEW} />
          <TrainGroupDateGridComponent formMode={FormMode.VIEW} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.ADD} />
          <TrainGroupDateGridComponent formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.EDIT} />
          <TrainGroupDateGridComponent formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent>
    </>
  );
}
