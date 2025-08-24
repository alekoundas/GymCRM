import DataTableComponent from "../../components/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableDto } from "../../model/DataTableDto";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { useEffect, useState } from "react";
import { useTrainGroupStore } from "../train-group/TrainGroupStore";
import { ColumnEditorOptions } from "primereact/column";
import { Dropdown } from "primereact/dropdown";
import { DayOfWeekEnum } from "../../enum/DayOfWeekEnum";
import { Calendar } from "primereact/calendar";
import { InputNumber, InputNumberChangeEvent } from "primereact/inputnumber";
import { RecurringTrainGroupTypeEnum } from "../../enum/RecurringTrainGroupTypeEnum";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupDateGrid({ formMode }: IField) {
  const { trainGroupDto, addTrainGroupDate, setTrainGroupDto } =
    useTrainGroupStore();
  const [editingRowType, setEditingRowType] =
    useState<RecurringTrainGroupTypeEnum | null>(null);
  const [editingRowIndex, setEditingRowIndex] = useState<number | null>(null);
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
      field: "repeatingTrainGroupType",
      header: "Type",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: null,
      cellEditor: (options: ColumnEditorOptions) => {
        if (options.field === "repeatingTrainGroupType") {
          return (
            <Dropdown
              value={options.value}
              // onChange={handleContactInformationChange}
              onChange={(e: any) => {
                options.editorCallback?.(e.target.value);
                setEditingRowType(e.value);
                // Update row data immediately
                if (editingRowIndex !== null) {
                  const updatedDates = [...trainGroupDto.trainGroupDates];
                  updatedDates[editingRowIndex] = {
                    ...updatedDates[editingRowIndex],
                    repeatingTrainGroupType: e.value,
                    fixedDay:
                      e.value === RecurringTrainGroupTypeEnum.FIXED_DAY
                        ? updatedDates[editingRowIndex].fixedDay || new Date()
                        : undefined,
                    recurrenceDayOfWeek:
                      e.value === RecurringTrainGroupTypeEnum.DAY_OF_WEEK
                        ? updatedDates[editingRowIndex].recurrenceDayOfWeek
                        : undefined,
                    recurrenceDayOfMonth:
                      e.value === RecurringTrainGroupTypeEnum.DAY_OF_MONTH
                        ? updatedDates[editingRowIndex].recurrenceDayOfMonth
                        : undefined,
                  };
                  setTrainGroupDto({
                    ...trainGroupDto,
                    trainGroupDates: updatedDates,
                  });
                }
              }}
              options={Object.keys(RecurringTrainGroupTypeEnum)}
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
        if (
          options.field === "fixedDay" &&
          editingRowType === RecurringTrainGroupTypeEnum.FIXED_DAY
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
        if (
          options.field === "recurrenceDayOfWeek" &&
          editingRowType === RecurringTrainGroupTypeEnum.DAY_OF_WEEK
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
        if (
          options.field === "recurrenceDayOfMonth" &&
          editingRowType === RecurringTrainGroupTypeEnum.DAY_OF_MONTH
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
    setEditingRowType(
      event.data.repeatingTrainGroupType ||
        RecurringTrainGroupTypeEnum.FIXED_DAY
    );
    setEditingRowIndex(event.index);
  };

  const onRowEditCancel = () => {
    setEditingRowType(null);
    setEditingRowIndex(null);
  };

  const onRowEditComplete = (e: any) => {
    const { newData, index } = e;
    const updatedDates = [...trainGroupDto.trainGroupDates];
    // Apply conditional logic to reset fields based on repeatingTrainGroupType
    const updatedRow: TrainGroupDateDto = {
      ...newData,
      fixedDay:
        newData.repeatingTrainGroupType ===
        RecurringTrainGroupTypeEnum.FIXED_DAY
          ? newData.fixedDay || new Date()
          : undefined,
      recurrenceDayOfWeek:
        newData.repeatingTrainGroupType ===
        RecurringTrainGroupTypeEnum.DAY_OF_WEEK
          ? newData.recurrenceDayOfWeek
          : undefined,
      recurrenceDayOfMonth:
        newData.repeatingTrainGroupType ===
        RecurringTrainGroupTypeEnum.DAY_OF_MONTH
          ? newData.recurrenceDayOfMonth
          : undefined,
    };
    updatedDates[index] = updatedRow;
    setTrainGroupDto({
      ...trainGroupDto,
      trainGroupDates: updatedDates,
    });
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
          id: -1,
          repeatingTrainGroupType: RecurringTrainGroupTypeEnum.FIXED_DAY,
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
