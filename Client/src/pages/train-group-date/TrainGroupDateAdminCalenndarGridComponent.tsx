import { Calendar } from "primereact/calendar";
import { ColumnEditorOptions } from "primereact/column";
import { Dropdown } from "primereact/dropdown";
import { InputNumber, InputNumberChangeEvent } from "primereact/inputnumber";
import { useState, useEffect } from "react";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DayOfWeekEnum } from "../../enum/DayOfWeekEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DateService } from "../../services/DateService";
import { useParams } from "react-router-dom";
import {
  DataTableSelectionSingleChangeEvent,
  DataTableValueArray,
} from "primereact/datatable";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";

interface IField extends DialogChildProps {}

export default function TrainGroupDateAdminCalenndarGridComponent({
  formMode,
}: IField) {
  const params = useParams();
  const {
    trainGroupDto,
    selectedTrainGroupDate,
    setSelectedTrainGroupDate,
    addTrainGroupDate,
    setTrainGroupDto,
    updateTrainGroupDto,
    resetSelectedTrainGroupDate,
  } = useTrainGroupStore();

  const [editingRows, setEditingRows] = useState<
    {
      rowId: number;
      type: TrainGroupDateTypeEnum;
    }[]
  >([]);

  useEffect(() => {}, []);
  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupDateDto>
  >({
    data: trainGroupDto.trainGroupDates,
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
    ],
  });

  // Update datatableDto when trainGroupDto.trainGroupDates changes
  useEffect(() => {
    setDatatableDto((prev) => ({
      ...prev,
      data: trainGroupDto.trainGroupDates,
      pageCount: trainGroupDto.trainGroupDates.length,
    }));
  }, [trainGroupDto.trainGroupDates]);

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "trainGroupDateType",
      header: "Type",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
      cellEditor: (options: ColumnEditorOptions) => {
        if (options.field === "trainGroupDateType") {
          return (
            <Dropdown
              value={options.value}
              // onChange={handleContactInformationChange}
              onChange={(e: any) => {
                options.editorCallback?.(e.target.value);
                const rowId = options.rowData["id"];
                editingRows.filter((x) => x.rowId === rowId)[0].type =
                  e.target.value;

                const row = editingRows.filter((x) => x.rowId === rowId)[0];
                const rowIndex = trainGroupDto.trainGroupDates.indexOf(
                  trainGroupDto.trainGroupDates.filter((x) => x.id === rowId)[0]
                );

                // Update row data immediately
                if (rowIndex !== null) {
                  const updatedDates = [...trainGroupDto.trainGroupDates];
                  updatedDates[rowIndex] = {
                    ...updatedDates[rowIndex],
                    trainGroupDateType: e.value,
                    fixedDay:
                      e.value === TrainGroupDateTypeEnum.FIXED_DAY
                        ? updatedDates[rowIndex].fixedDay
                        : undefined,
                    recurrenceDayOfWeek:
                      e.value === TrainGroupDateTypeEnum.DAY_OF_WEEK
                        ? updatedDates[rowIndex].recurrenceDayOfWeek
                        : undefined,
                    recurrenceDayOfMonth:
                      e.value === TrainGroupDateTypeEnum.DAY_OF_MONTH
                        ? updatedDates[rowIndex].recurrenceDayOfMonth
                        : undefined,
                  };
                  setTrainGroupDto({
                    ...trainGroupDto,
                    trainGroupDates: updatedDates,
                  });
                }
              }}
              options={Object.keys(TrainGroupDateTypeEnum)}
              optionLabel="type"
              placeholder="Select Type"
              className="w-full"
              checkmark={true}
              highlightOnSelect={true}
            />
          );
        }
      },
    },
    {
      field: "fixedDay",
      header: "Fixed Day",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },

      body: (rowData: TrainGroupDateDto) => {
        // return rowData.fixedDay; // Convert Date to string
        if (rowData.fixedDay) {
          const date = new Date(rowData.fixedDay);
          return (
            date.getDate() +
            "/" +
            (date.getMonth() + 1) +
            "/" +
            date.getFullYear()
          );
        }
      },
      cellEditor: (options: ColumnEditorOptions) => {
        const editingRow = editingRows.filter(
          (x) => x.rowId === options.rowData.id
        )[0];

        if (
          editingRow &&
          options.field === "fixedDay" &&
          editingRow.type === TrainGroupDateTypeEnum.FIXED_DAY
        ) {
          return (
            <Calendar
              value={new Date(options.value)}
              onChange={(e: any) => {
                options.editorCallback?.(e.target.value);
              }}
              showIcon={true}
              minDate={new Date(new Date().setHours(0, 0, 0, 0))} // Prevent selecting past dates
              className="w-full"
              dateFormat="dd/mm/yy"
            />
          );
        }
      },
    },
    {
      field: "recurrenceDayOfWeek",
      header: "Recurrence Day Of Week",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: (rowData: TrainGroupDateDto) => {
        return DateService.getDayOfWeekFromDate(rowData.recurrenceDayOfWeek);
      },
      cellEditor: (options: ColumnEditorOptions) => {
        const editingRow = editingRows.filter(
          (x) => x.rowId === options.rowData.id
        )[0];

        if (
          editingRow &&
          options.field === "recurrenceDayOfWeek" &&
          editingRow.type === TrainGroupDateTypeEnum.DAY_OF_WEEK
        ) {
          return (
            <Dropdown
              value={DateService.getDayOfWeekFromDate(options.value)}
              options={Object.keys(DayOfWeekEnum)}
              onChange={(e: any) => {
                if (e.target.value) {
                  const updatedDate: Date | undefined =
                    DateService.getDateFromDayOfWeek(e.target.value);

                  if (updatedDate) {
                    options.editorCallback?.(updatedDate);
                  }
                }
              }}
              placeholder="Select Type"
              className="w-full"
              checkmark={true}
              highlightOnSelect={true}
            />
          );
        }
      },
    },
    {
      field: "recurrenceDayOfMonth",
      header: "Recurrence Day Of Month",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: (data: TrainGroupDateDto) => {
        if (data.recurrenceDayOfMonth)
          return new Date(data.recurrenceDayOfMonth).getDate();
      },
      cellEditor: (options: ColumnEditorOptions) => {
        const editingRow = editingRows.filter(
          (x) => x.rowId === options.rowData.id
        )[0];

        if (
          editingRow &&
          options.field === "recurrenceDayOfMonth" &&
          editingRow.type === TrainGroupDateTypeEnum.DAY_OF_MONTH
        ) {
          return (
            <InputNumber
              value={new Date(options.value).getDate()}
              onChange={(e: InputNumberChangeEvent) => {
                if (e.value)
                  options.editorCallback?.(
                    new Date(2000, 0, e.value, 0, 0, 0, 0)
                  );
              }}
              min={0}
              max={31}
            />
          );
        }
      },
    },
  ];

  const onRowEditInit = (event: { data: TrainGroupDateDto; index: number }) => {
    editingRows?.push({
      rowId: event.data.id,
      type: event.data.trainGroupDateType ?? TrainGroupDateTypeEnum.FIXED_DAY,
    });

    setEditingRows(editingRows);
  };

  const onRowEditCancel = (event: {
    data: TrainGroupDateDto;
    index: number;
  }) => {
    const rowId = event.data.id;
    const rows = editingRows.filter((x) => x.rowId !== rowId);
    setEditingRows(rows);
  };

  const onAfterDataLoaded = (data: DataTableDto<TrainGroupDateDto> | null) => {
    if (data) {
      // trainGroupDto.trainGroupDates = data.data;
      updateTrainGroupDto({ trainGroupDates: data.data });
      data.data = [];
    }
    return data;
  };

  const onRowEditComplete = (e: any) => {
    const { newData, index } = e;
    const updatedDates = [...trainGroupDto.trainGroupDates];
    // Apply conditional logic to reset fields based on trainGroupDateType
    const updatedRow: TrainGroupDateDto = {
      ...newData,
      fixedDay:
        newData.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
          ? newData.fixedDay
          : undefined,
      recurrenceDayOfWeek:
        newData.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
          ? newData.recurrenceDayOfWeek
          : undefined,
      recurrenceDayOfMonth:
        newData.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
          ? newData.recurrenceDayOfMonth
          : undefined,
    };
    updatedDates[index] = updatedRow;
    setTrainGroupDto({
      ...trainGroupDto,
      trainGroupDates: updatedDates,
    });

    const rowId = newData.id;
    const rows = editingRows.filter((x) => x.rowId !== rowId);
    setEditingRows(rows);
  };

  const onSelect = (
    e: DataTableSelectionSingleChangeEvent<DataTableValueArray>
  ) => {
    if (e.value) {
      setSelectedTrainGroupDate(e.value as TrainGroupDateDto);
    } else {
      resetSelectedTrainGroupDate();
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDateDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        break;
      case ButtonTypeEnum.ADD:
        const newRow: TrainGroupDateDto = {
          id:
            (trainGroupDto.trainGroupDates.filter((x) => x.id < 0).length + 1) *
            -1,
          trainGroupDateType: TrainGroupDateTypeEnum.FIXED_DAY,
          fixedDay: undefined,
          recurrenceDayOfWeek: undefined,
          recurrenceDayOfMonth: undefined,
          trainGroupId: trainGroupDto.id > 0 ? trainGroupDto.id : -1,
          trainGroupParticipants: [],
          trainGroupDateCancellationSubscribers: [],
        };
        addTrainGroupDate(newRow);
        break;
      case ButtonTypeEnum.EDIT:
        break;
      case ButtonTypeEnum.DELETE:
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        controller="TrainGroupDates"
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={formMode ?? FormMode.VIEW}
        dataTableColumns={dataTableColumns}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        enableAddAction={formMode !== FormMode.VIEW}
        editMode={
          formMode !== FormMode.VIEW ? DataTableEditModeEnum.ROW : undefined
        }
        onButtonClick={onDataTableClick}
        onRowEditComplete={onRowEditComplete}
        onRowEditInit={onRowEditInit}
        onRowEditCancel={onRowEditCancel}
        onAfterDataLoaded={onAfterDataLoaded}
        authorize={true}
      />
    </>
  );
}
