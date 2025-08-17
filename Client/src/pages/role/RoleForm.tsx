import React, { useState } from "react";
import DataTableComponent from "../../components/datatable/DataTableComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { ClaimDto } from "../../model/ClaimDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableDto } from "../../model/DataTableDto";
import { DataTableEditModeEnum } from "../../enum/DataTableEditModeEnum";
import { InputSwitch } from "primereact/inputswitch";
import { ColumnEditorOptions, ColumnEvent } from "primereact/column";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import { IdentityRoleDto } from "../../model/IdentityRoleDto";
import ApiService from "../../services/ApiService";
import { ToastService } from "../../services/ToastService";

interface IField {
  data: IdentityRoleDto;
  formMode: FormMode;
  onEnableSaveButton?: () => void;
  onDisableSaveButton?: () => void;
  onAfterSave: () => void;
  triggerSaveForm?: () => void;
}

export default function RoleForm({
  data,
  formMode,
  onEnableSaveButton,
  onDisableSaveButton,
  onAfterSave,
  triggerSaveForm,
}: IField) {
  const [identityRoleDto, setIdentityRoleDto] = useState(data);
  const isFirstRender = React.useRef(true);
  let getDataFromDataTable: () => ClaimDto[] = () => [];

  //
  // Handle save triggered from parent.
  //
  React.useEffect(() => {
    // Skip the first run
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }
    if (triggerSaveForm) {
      handleSave();
    }
  }, [triggerSaveForm]);

  const handleSave = () => {
    if (getDataFromDataTable) {
      identityRoleDto.claims = getDataFromDataTable();
      setIdentityRoleDto({ ...identityRoleDto });
      if (formMode == FormMode.EDIT) {
        ApiService.update("roles", identityRoleDto, identityRoleDto.id)
          .then((response) => {
            if (response) {
              ToastService.showSuccess("");
              return;
            }
            ToastService.showError("");
          })
          .then(onAfterSave);
      } else {
        ApiService.create("roles", identityRoleDto)
          .then((response) => {
            if (response) {
              ToastService.showSuccess("");
              return;
            }
            ToastService.showError("");
          })
          .then(onAfterSave);
      }
    }
  };

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: ClaimDto) => {
    console.log(rowData);
    console.log(buttonType);
  };

  const datatableDto: DataTableDto<ClaimDto> = {
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    multiSortMeta: [],
    filters: {
      roleName: {
        value: identityRoleDto.name,
        matchMode: "contains",
      },
    },
  };

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
    if (onEnableSaveButton) {
      onEnableSaveButton();
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
      editor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: onDisableSaveButton,
    },
    {
      field: "add",
      header: "Add",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.add),
      editor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: onDisableSaveButton,
    },
    {
      field: "edit",
      header: "Edit",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.edit),
      editor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: onDisableSaveButton,
    },

    {
      field: "delete",
      header: "Delete",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "1%" },
      body: (rowData: ClaimDto) => cellBody(rowData.delete),
      editor: cellEditor,
      onCellEditComplete: onCellEditComplete,
      onCellEditInit: onDisableSaveButton,
    },
  ];

  const handleChange = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    identityRoleDto[name] = value;
    setIdentityRoleDto({ ...identityRoleDto });
  };

  // Load initial data.
  React.useEffect(() => {
    if (formMode === FormMode.EDIT || formMode === FormMode.VIEW) {
      ApiService.get<IdentityRoleDto>("roles", identityRoleDto.id).then(
        (result) => {
          if (result) {
            setIdentityRoleDto({ ...result });
          }
        }
      );
    }
  }, []);

  return (
    <>
      <Card title="Role ">
        <form>
          <div className="flex align-items-center justify-content-center">
            <div className="field">
              <label htmlFor="name">Role Name</label>
              <InputText
                id="name"
                name="name"
                value={identityRoleDto.name}
                onChange={handleChange}
                disabled={formMode !== FormMode.ADD}
              />
            </div>
          </div>
        </form>
      </Card>

      <DataTableComponent
        controller="claims"
        dataTable={datatableDto}
        dataTableColumns={dataTableColumns}
        editMode={
          formMode !== FormMode.VIEW ? DataTableEditModeEnum.CELL : undefined
        }
        onButtonClick={onDataTableClick}
        triggerRequestData={(fn) => (getDataFromDataTable = fn)}
      />
    </>
  );
}
