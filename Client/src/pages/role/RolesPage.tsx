import { useRef, useState } from "react";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { useRoleStore } from "../../stores/RoleStore";
import { RoleDto } from "../../model/entities/role/RoleDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import ApiService from "../../services/ApiService";
import RoleFormComponent from "./RoleFormComponent";
import RoleGridComponent from "./RoleGridComponent";

export default function RolesPage() {
  const { roleDto, setRoleDto, resetRoleDto } = useRoleStore();
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<RoleDto>) => void) | undefined
  >(undefined);
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
  const [datatableDto, setDatatableDto] = useState<DataTableDto<RoleDto>>({
    data: [],
    first: 0,
    rows: 10,
    page: 1,
    pageCount: 0,
    filters: [
      { fieldName: "Value", filterType: "contains" },
      { fieldName: "Description", filterType: "contains" },
    ],
    dataTableSorts: [],
  });

  const dataTableColumns: DataTableColumns[] = [
    {
      field: "name",
      header: "Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: null,
    },
    {
      field: "normalizedName",
      header: "Normalized Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
      body: null,
    },
  ];

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: any) => {
    if (rowData) setRoleDto({ ...rowData });
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        setViewDialogVisibility(true);
        break;
      case ButtonTypeEnum.ADD:
        resetRoleDto();
        setAddDialogVisibility(true);
        break;
      case ButtonTypeEnum.EDIT:
        setEditDialogVisibility(true);
        break;
      case ButtonTypeEnum.DELETE:
        // setDeleteDialogVisibility(true);
        break;

      default:
        break;
    }
  };

  const OnSaveAdd = async () => {
    const response = await ApiService.create("roles", roleDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetRoleDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const OnSaveEdit = async () => {
    const response = await ApiService.update("roles", roleDto, roleDto.id);

    if (response) {
      dialogControlEdit.hideDialog();
      resetRoleDto();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  return (
    <>
      <Card title="Roles">
        <div className="card">
          <DataTableComponent
            controller="roles"
            dataTableDto={datatableDto}
            setDataTableDto={setDatatableDto}
            formMode={FormMode.EDIT}
            onButtonClick={onDataTableClick}
            enableGridRowActions={true}
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            enableAddAction={true}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={triggerRefreshDataTable}
            authorize={true}
          />
        </div>
      </Card>

      {/*                                     */}
      {/*               View Role             */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <RoleFormComponent formMode={FormMode.VIEW} />
          <RoleGridComponent formMode={FormMode.VIEW} />
        </div>
      </GenericDialogComponent>

      {/*                                      */}
      {/*               Add Role               */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <RoleFormComponent formMode={FormMode.ADD} />
          <RoleGridComponent formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*             Edit Role               */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <RoleFormComponent formMode={FormMode.EDIT} />
          <RoleGridComponent formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent>

      {/* Delete Dialog */}
      {/* <DeleteDialogComponent
        onAfterRowDeletion={afterSave}
        triggerDialogVisibility={(fn) => (setDeleteDialogVisibility = fn)}
        id={identityRoleDto.id}
        name={identityRoleDto.name}
      /> */}
    </>
  );
}
