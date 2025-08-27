import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { Dialog } from "primereact/dialog";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupForm from "./TrainGroupForm";
import TrainGroupDateGrid from "../train-group-date/TrainGroupDateGrid";
import { TrainGroupDto } from "../../model/TrainGroupDto";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { DayOfWeekEnum } from "../../enum/DayOfWeekEnum";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DateService } from "../../services/DateService";

export default function TrainGroupAdminPage() {
  const [isViewModalVisible, setViewModalVisibility] = useState(false); // Dialog visibility
  const [isAddModalVisible, setAddModalVisibility] = useState(false); // Dialog visibility
  const [isEditModalVisible, setEditModalVisibility] = useState(false); // Dialog visibility

  const { trainGroupDto, resetTrainGroupDto, setTrainGroupDto } =
    useTrainGroupStore();

  const dialogControlView: DialogControl = {
    showdialog: () => setViewModalVisibility(true),
    hidedialog: () => setViewModalVisibility(false),
  };

  const dialogControlAdd: DialogControl = {
    showdialog: () => setAddModalVisibility(true),
    hidedialog: () => setAddModalVisibility(false),
  };

  const dialogControlEdit: DialogControl = {
    showdialog: () => setEditModalVisibility(true),
    hidedialog: () => setEditModalVisibility(false),
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

  const OnSaveAdd = async () => {
    // Update recurrenceDayOfWeek value to UTC
    // Update recurrenceDayOfMonth value to UTC
    // const updatedTrainGroupDto: TrainGroupDto = {
    //   ...trainGroupDto,
    //   trainGroupDates: trainGroupDto.trainGroupDates.map(
    //     (dateDto: TrainGroupDateDto) => {
    //       if (dateDto.recurrenceDayOfWeek) {
    //         const utcDayOfWeek = DateService.getUTCDayOfWeek(
    //           dateDto.recurrenceDayOfWeek
    //         );
    //         dateDto.recurrenceDayOfWeek = utcDayOfWeek;
    //         return dateDto;
    //       }

    //       if (dateDto.recurrenceDayOfMonth) {
    //         const utcDayOfMonth = DateService.getUTCDayOfMonth(
    //           dateDto.recurrenceDayOfMonth
    //         );
    //         dateDto.recurrenceDayOfMonth = utcDayOfMonth;
    //         return dateDto;
    //       }

    //       return dateDto;
    //     }
    //   ),
    // };

    const response = await ApiService.create("trainGroup", trainGroupDto);

    if (response) {
      dialogControlAdd.hidedialog();
      resetTrainGroupDto();
    }
  };

  const OnSaveEdit = async () => {
    // Update recurrenceDayOfWeek value to UTC
    // Update recurrenceDayOfMonth value to UTC
    // const updatedTrainGroupDto: TrainGroupDto = {
    //   ...trainGroupDto,
    //   trainGroupDates: trainGroupDto.trainGroupDates.map(
    //     (dateDto: TrainGroupDateDto) => {
    //       if (dateDto.recurrenceDayOfWeek) {
    //         const utcDayOfWeek = DateService.getUTCDayOfWeek(
    //           dateDto.recurrenceDayOfWeek
    //         );
    //         dateDto.recurrenceDayOfWeek = utcDayOfWeek;
    //         return dateDto;
    //       }

    //       if (dateDto.recurrenceDayOfMonth) {
    //         const utcDayOfMonth = DateService.getUTCDayOfMonth(
    //           dateDto.recurrenceDayOfMonth
    //         );
    //         dateDto.recurrenceDayOfMonth = utcDayOfMonth;
    //         return dateDto;
    //       }

    //       return dateDto;
    //     }
    //   ),
    // };

    const response = await ApiService.update(
      "trainGroup",
      trainGroupDto,
      trainGroupDto.id
    );

    if (response) {
      dialogControlEdit.hidedialog();
      resetTrainGroupDto();
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDto
  ) => {
    if (rowData) setTrainGroupDto({ ...rowData });
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        dialogControlView.showdialog();
        break;
      case ButtonTypeEnum.ADD:
        dialogControlAdd.showdialog();
        break;
      case ButtonTypeEnum.EDIT:
        dialogControlEdit.showdialog();
        break;
      case ButtonTypeEnum.DELETE:
        // setDeleteDialogVisibility(true);
        break;
      case ButtonTypeEnum.SAVE:
        // triggerFormSave();
        dialogControlAdd.hidedialog();
        dialogControlEdit.hidedialog();
        // if (onRefreshDataTable.current) onRefreshDataTable.current();
        break;

      default:
        break;
    }
  };

  return (
    <>
      <Card title="Makers">
        <div className="card">
          <DataTableComponent
            dataTable={datatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            controller="TrainGroup"
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={true}
            dataTableColumns={dataTableColumns}
            // triggerRefreshData={onRefreshDataTable}
          />
        </div>
      </Card>

      {/*                                     */}
      {/*           View Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isViewModalVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <TrainGroupForm formMode={FormMode.VIEW} />
          <TrainGroupDateGrid formMode={FormMode.VIEW} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddModalVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupForm formMode={FormMode.ADD} />
          <TrainGroupDateGrid formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditModalVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupForm formMode={FormMode.EDIT} />
          <TrainGroupDateGrid formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent>
    </>
  );
}
