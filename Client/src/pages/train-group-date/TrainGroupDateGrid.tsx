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
import { DataTableDto } from "../../model/DataTableDto";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupDateGrid({ formMode }: IField) {
  const { trainGroupDto, addTrainGroupDate, setTrainGroupDto } =
    useTrainGroupStore();

  const [editingRows, setEditingRows] = useState<
    {
      rowId: number;
      type: TrainGroupDateTypeEnum;
    }[]
  >([]);

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupDateDto>
  >({
    data: trainGroupDto.trainGroupDates,
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    multiSortMeta: [],
    filters: {
      title: { value: "", matchMode: "contains" },
    },
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
                        ? updatedDates[rowIndex].fixedDay || new Date()
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

      body: (rowData: TrainGroupDateDto) =>
        rowData.fixedDay ? rowData.fixedDay.toLocaleDateString() : "", // Convert Date to string
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
              value={options.value}
              onChange={(e: any) => options.editorCallback?.(e.target.value)}
              showIcon={true}
              minDate={new Date()} // Prevent selecting past dates
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
      body: null,
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
              value={options.value}
              options={Object.keys(DayOfWeekEnum)}
              onChange={(e: any) => options.editorCallback?.(e.target.value)}
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
      body: null,
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
              value={options.value}
              onChange={(e: InputNumberChangeEvent) =>
                options.editorCallback?.(e.value)
              }
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

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupDateDto
  ) => {
    if (rowData) {
      // data.indexOf()
      // setTrainGroupDateDto({ ...rowData });
    }
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        break;
      case ButtonTypeEnum.ADD:
        const newRow: TrainGroupDateDto = {
          id: (trainGroupDto.trainGroupDates.length + 1) * -1,
          trainGroupDateType: TrainGroupDateTypeEnum.FIXED_DAY,
          fixedDay: undefined,
          recurrenceDayOfWeek: undefined,
          recurrenceDayOfMonth: undefined,
          trainGroupId: trainGroupDto.id > 0 ? trainGroupDto.id : 0,
          trainGroupParticipants: [],
          trainGroupCancellationSubscribers: [],
        };
        addTrainGroupDate(newRow);
        break;
      case ButtonTypeEnum.EDIT:
        break;
      case ButtonTypeEnum.DELETE:
        break;
      case ButtonTypeEnum.SAVE:
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        onButtonClick={onDataTableClick}
        controller="TrainGroupDate"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        enableAddAction={true}
        dataTable={datatableDto}
        dataTableColumns={dataTableColumns}
        editMode={DataTableEditModeEnum.ROW}
        onRowEditComplete={onRowEditComplete}
        onRowEditInit={onRowEditInit}
        onRowEditCancel={onRowEditCancel}
        formMode={formMode}
      />
    </>
  );
}
