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
import ClaimGridComponent from "../claim/ClaimGridComponent";

export default function RolesPage() {
  const { roleDto, setRoleDto, resetRoleDto } = useRoleStore();
  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<RoleDto>) => void) | undefined
  >(undefined);
  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false); // Dialog visibility
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

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
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<DataTableDto<RoleDto>>({
    ...new DataTableDto(),
    filters: [
      { fieldName: "name", filterType: "contains" },
      { fieldName: "normalizedName", filterType: "contains" },
    ],
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.ADD,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
    ];
  };

  const dataTableColumns: DataTableColumns<RoleDto>[] = [
    {
      field: "name",
      header: "Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
    },
    {
      field: "normalizedName",
      header: "Normalized Name",
      sortable: true,
      filter: true,
      filterPlaceholder: "Search",
      style: { width: "30%" },
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
        setDeleteDialogVisibility(true);
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

  const onDelete = async (): Promise<void> => {
    const response = await ApiService.delete("roles", roleDto.id);

    dialogControlDelete.hideDialog();
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
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
            filterDisplay={DataTableFilterDisplayEnum.ROW}
            dataTableColumns={dataTableColumns}
            triggerRefreshData={triggerRefreshDataTable}
            authorize={true}
            availableGridRowButtons={availableGridRowButtons()}
          />
        </div>
      </Card>

      {/*                                     */}
      {/*               View Role             */}
      {/*                                     */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <RoleFormComponent />
          <ClaimGridComponent />
        </div>
      </GenericDialogComponent>

      {/*                                      */}
      {/*               Add Role               */}
      {/*                                      */}

      <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
        formMode={FormMode.ADD}
      >
        <div className="w-full">
          <RoleFormComponent />
          <ClaimGridComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*             Edit Role               */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
        formMode={FormMode.EDIT}
      >
        <div className="w-full">
          <RoleFormComponent />
          <ClaimGridComponent />
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Delete Train Group           */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDelete}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>Are you sure?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
