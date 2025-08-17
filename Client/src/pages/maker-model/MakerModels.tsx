import { useRef, useState } from "react";
import DataTableComponent from "../../components/datatable/DataTableComponent";
import DeleteDialogComponent from "../../components/dialog/DeleteDialogComponent";
import EditDialogComponent from "../../components/dialog/EditDialogComponent";
import ViewDialogComponent from "../../components/dialog/ViewDialogComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableDto } from "../../model/DataTableDto";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import AddDialogComponent from "../../components/dialog/AddDialogComponent";
import { Card } from "primereact/card";
import { MakerModelDto } from "../../model/MakerModelDto";
import MakerModelForm from "./MakerModelForm";

export default function MakerModels() {
  const [makerModelDto, setMakerModelDto] = useState(new MakerModelDto());

  let setDeleteDialogVisibility: (newValue: boolean) => void = () => {};
  let setEditDialogVisibility: (newValue: boolean) => void = () => {};
  let setViewDialogVisibility: (newValue: boolean) => void = () => {};
  let setAddDialogVisibility: (newValue: boolean) => void = () => {};
  // let setSaveDisableStateAdd: (newValue: boolean) => void = () => {};
  // let setSaveDisableStateEdit: (newValue: boolean) => void = () => {};
  let triggerFormSave: () => void = () => {};
  const onRefreshDataTable = useRef<(() => void) | undefined>(undefined);

  const formMode: FormMode = window.location.href.endsWith("/add")
    ? FormMode.ADD
    : window.location.href.endsWith("/edit")
    ? FormMode.EDIT
    : FormMode.VIEW;

  const datatableDto: DataTableDto<MakerModelDto> = {
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    multiSortMeta: [],
    filters: {
      title: { value: "", matchMode: "contains" },
      makerId: { value: "", matchMode: "contains" },
    },
  };

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "title",
      header: "Title",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: null,
    },
    {
      field: "makerId",
      header: "Maker Id",
      sortable: formMode !== FormMode.ADD,
      filter: formMode !== FormMode.ADD,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: null,
    },
  ];

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: MakerModelDto
  ) => {
    if (rowData) setMakerModelDto({ ...rowData });
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        setViewDialogVisibility(true);
        break;
      case ButtonTypeEnum.ADD:
        setAddDialogVisibility(true);
        break;
      case ButtonTypeEnum.EDIT:
        setEditDialogVisibility(true);
        break;
      case ButtonTypeEnum.DELETE:
        setDeleteDialogVisibility(true);
        break;
      case ButtonTypeEnum.SAVE:
        triggerFormSave();
        setAddDialogVisibility(false);
        setEditDialogVisibility(false);
        if (onRefreshDataTable.current) onRefreshDataTable.current();
        break;

      default:
        break;
    }
  };

  const afterSave = () => {
    if (onRefreshDataTable.current) onRefreshDataTable.current();
    setDeleteDialogVisibility(false);
  };

  // Link onSave event button to form Save function.
  const [triger, settriger] = useState(0);
  let triggerSave = () => {
    settriger(triger + 1);
    triggerFormSave();
    setAddDialogVisibility(false);
    setEditDialogVisibility(false);
  };

  return (
    <>
      <Card title="Maker Models">
        <div className="card">
          <DataTableComponent
            onButtonClick={onDataTableClick}
            controller="makermodels"
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={true}
            dataTable={datatableDto}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={onRefreshDataTable}
          />
        </div>
      </Card>

      {/* Delete Modal */}
      <DeleteDialogComponent
        onAfterRowDeletion={afterSave}
        triggerDialogVisibility={(fn) => (setDeleteDialogVisibility = fn)}
        id={makerModelDto.id}
        name={makerModelDto.title}
      />

      {/* View Modal */}
      <ViewDialogComponent
        triggerDialogVisibility={(fn) => (setViewDialogVisibility = fn)}
      >
        <MakerModelForm
          data={makerModelDto}
          formMode={FormMode.VIEW}
          onAfterSave={afterSave}
        />
      </ViewDialogComponent>

      {/* Edit Modal */}
      <EditDialogComponent
        onSaveButtonClick={triggerSave}
        triggerDialogVisibility={(fn) => (setEditDialogVisibility = fn)}
        // triggerSaveDisable={(fn) => (setSaveDisableStateEdit = fn)}
        // triggerSaveEnable={(fn) => (setSaveDisableStateEdit = fn)}
      >
        <MakerModelForm
          data={makerModelDto}
          formMode={FormMode.EDIT}
          onAfterSave={afterSave}
          // onDisableSaveButton={() => setSaveDisableStateEdit(false)}
          // onEnableSaveButton={() => setSaveDisableStateEdit(true)}
          triggerSaveForm={triggerFormSave}
        />
      </EditDialogComponent>

      {/* Add Modal */}
      <AddDialogComponent
        onSaveButtonClick={triggerSave}
        triggerDialogVisibility={(fn) => (setAddDialogVisibility = fn)}
        // triggerSaveDisable={(fn) => (setSaveDisableStateAdd = fn)}
        // triggerSaveEnable={(fn) => (setSaveDisableStateAdd = fn)}
      >
        <MakerModelForm
          data={makerModelDto}
          formMode={FormMode.ADD}
          onAfterSave={afterSave}
          // onDisableSaveButton={() => setSaveDisableStateAdd(false)}
          // onEnableSaveButton={() => setSaveDisableStateAdd(true)}
          triggerSaveForm={triggerFormSave}
        />
      </AddDialogComponent>
    </>
  );
}
