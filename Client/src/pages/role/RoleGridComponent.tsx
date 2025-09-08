import { FormMode } from "../../enum/FormMode";
import { ClaimDto } from "../../model/ClaimDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { InputSwitch } from "primereact/inputswitch";
import { ColumnEditorOptions, ColumnEvent } from "primereact/column";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { useRoleStore } from "../../stores/RoleStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";

interface IField extends DialogChildProps {}

export default function RoleGridComponent({
  formMode,
  toggleDialogSave,
}: IField) {
  const { roleDto, setRoleDto } = useRoleStore();

  const [datatableDto, setDatatableDto] = useState<DataTableDto<ClaimDto>>({
    data: roleDto.claims,
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    filters: [
      { fieldName: "RoleName", filterType: "contains", value: roleDto.name },
    ],
    dataTableSorts: [],
  });

  const cellBody = (value: boolean) => {
    return (
      <InputSwitch
        checked={value}
        disabled
      />
    );
  };

  const cellEditor = (options: ColumnEditorOptions) => {
    return (
      <InputSwitch
        checked={options.value}
        onChange={(e) =>
          options.editorCallback ? options.editorCallback(e.value) : null
        }
      />
    );
  };

  const onCellEditComplete = (e: ColumnEvent) => {
    let { rowData, newValue, field } = e;
    rowData[field] = newValue;
    if (toggleDialogSave) {
      toggleDialogSave(true);
    }
  };
  const dataTableColumns: DataTableColumns[] = [
    {
      field: "controller",
      header: "Controller",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: null,
    },
    {
      field: "view",
      header: "View",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.view),
      cellEditor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: () => {
        if (toggleDialogSave) toggleDialogSave(false);
      },
    },
    {
      field: "add",
      header: "Add",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.add),
      cellEditor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: () => {
        if (toggleDialogSave) toggleDialogSave(false);
      },
    },
    {
      field: "edit",
      header: "Edit",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.edit),
      cellEditor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: () => {
        if (toggleDialogSave) toggleDialogSave(false);
      },
    },
    {
      field: "delete",
      header: "Delete",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.delete),
      cellEditor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: () => {
        if (toggleDialogSave) toggleDialogSave(false);
      },
    },
  ];

  const onAfterDataLoaded = (data: DataTableDto<ClaimDto> | null) => {
    if (data) {
      roleDto.claims = data.data;
      setRoleDto(roleDto);
      // data.data = [];
    }
    return data;
  };

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: ClaimDto) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        break;
      case ButtonTypeEnum.ADD:
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
        controller="claims"
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={formMode === FormMode.VIEW ? formMode : FormMode.EDIT}
        dataTableColumns={dataTableColumns}
        // filterDisplay={}
        enableGridRowActions={false}
        editMode={
          formMode !== FormMode.VIEW ? DataTableEditModeEnum.CELL : undefined
        }
        onAfterDataLoaded={onAfterDataLoaded}
        onButtonClick={onDataTableClick}
      />
    </>
  );
}
