import { useRef, useState } from "react";
import { Tag } from "primereact/tag";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { TrainGroupAttendanceDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceDto";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  userId: string;
}

// Every session this member has actually trained. Read only on the profile - taking
// an attendance back is an administrator's job, and it is done from the admin screen
// where the consequence to their remaining subscriptions is in front of you.
export default function TrainGroupAttendanceMemberTabComponent({
  userId,
}: IField) {
  const { t } = useTranslator();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupAttendanceDto>) => void) | undefined
  >(undefined);

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupAttendanceDto>
  >({
    ...new DataTableDto(),
    rows: 10,
    // Capital U: the server turns this one into a Guid by name. A member only ever
    // gets their own rows back anyway, but an administrator looking at somebody's
    // profile would otherwise see everyone's.
    filters: [{ fieldName: "UserId", value: userId, filterType: "equals" }],
    dataTableSorts: [{ field: "attendanceDate", order: -1 }],
  });

  const formatDate = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  // A stored wall clock, shown as the clock it is.
  const formatClock = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return (
      date.getUTCHours().toString().padStart(2, "0") +
      ":" +
      date.getUTCMinutes().toString().padStart(2, "0")
    );
  };

  const dataTableColumns: DataTableColumns<TrainGroupAttendanceDto>[] = [
    {
      field: "attendanceDate",
      header: t("Attendance date"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatDate(rowData.attendanceDate),
      style: { width: "22%" },
    },
    {
      // The snapshot rather than the group, so a session still says what it was after
      // the group has been renamed or deleted.
      field: "trainGroupTitle",
      header: t("Train Group"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => (
        <div className="flex align-items-center gap-2">
          <span>{rowData.trainGroupTitle}</span>
          {!rowData.trainGroupId && (
            <Tag
              severity="warning"
              value={t("Deleted")}
            />
          )}
        </div>
      ),
      style: { width: "36%" },
    },
    {
      field: "trainerFullName",
      header: t("Trainer"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "26%" },
    },
    {
      field: "trainGroupStartOn",
      header: t("Start On"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatClock(rowData.trainGroupStartOn),
      style: { width: "16%" },
    },
  ];

  return (
    <DataTableComponent
      controller="TrainGroupAttendances"
      dataTableDto={datatableDto}
      setDataTableDto={setDatatableDto}
      formMode={FormMode.VIEW}
      onButtonClick={() => {}}
      filterDisplay={DataTableFilterDisplayEnum.ROW}
      dataTableColumns={dataTableColumns}
      triggerRefreshData={triggerRefreshDataTable}
      availableGridRowButtons={[] as ButtonTypeEnum[]}
    />
  );
}
