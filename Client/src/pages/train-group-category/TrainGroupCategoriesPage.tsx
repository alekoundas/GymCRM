import { useRef, useState } from "react";
import { Card } from "primereact/card";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { TrainGroupCategoryDto } from "../../model/entities/train-group-category/TrainGroupCategoryDto";
import { useApiService } from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";
import TrainGroupCategoryFormComponent from "./TrainGroupCategoryFormComponent";

// The categories train groups can be put in. Each one becomes a tab on the admin
// calendar, next to "All".
export default function TrainGroupCategoriesPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<TrainGroupCategoryDto>) => void) | undefined
  >(undefined);

  const [dto, setDto] = useState<TrainGroupCategoryDto>(new TrainGroupCategoryDto());
  const update = (updates: Partial<TrainGroupCategoryDto>) =>
    setDto((previous) => ({ ...previous, ...updates }));

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false);
  const [isAddDialogVisible, setAddDialogVisibility] = useState(false);
  const [isEditDialogVisible, setEditDialogVisibility] = useState(false);
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false);

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

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<TrainGroupCategoryDto>
  >({
    ...new DataTableDto(),
    // The datatable builds its filter metadata from this list, so a column marked
    // filterable without an entry here renders a box that collects nothing.
    filters: [{ fieldName: "name", filterType: "contains" }],
    // sorts is what the server orders by; dataTableSorts only draws the arrow.
    sorts: [{ fieldName: "name", order: 1 }],
    dataTableSorts: [{ field: "name", order: 1 }],
  });

  const dataTableColumns: DataTableColumns<TrainGroupCategoryDto>[] = [
    {
      field: "name",
      header: t("Category"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "100%" },
    },
  ];

  const availableGridRowButtons = (): ButtonTypeEnum[] => {
    const result: ButtonTypeEnum[] = [ButtonTypeEnum.VIEW];

    // ADD is what puts the button in the grid header, not on a row.
    if (TokenService.isUserAllowed("TrainGroupCategories_Add"))
      result.push(ButtonTypeEnum.ADD);
    if (TokenService.isUserAllowed("TrainGroupCategories_Edit"))
      result.push(ButtonTypeEnum.EDIT);
    if (TokenService.isUserAllowed("TrainGroupCategories_Delete"))
      result.push(ButtonTypeEnum.DELETE);

    return result;
  };

  const refresh = () => {
    if (triggerRefreshDataTable.current) triggerRefreshDataTable.current(datatableDto);
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: TrainGroupCategoryDto
  ) => {
    if (rowData) setDto({ ...rowData });

    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        dialogControlView.showDialog();
        break;
      case ButtonTypeEnum.ADD:
        setDto(new TrainGroupCategoryDto());
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        dialogControlEdit.showDialog();
        break;
      case ButtonTypeEnum.DELETE:
        dialogControlDelete.showDialog();
        break;
      default:
        break;
    }
  };

  const onSaveAdd = async (): Promise<void> => {
    const response = await apiService.create("TrainGroupCategories", {
      name: dto.name.trim(),
    });

    if (response) {
      dialogControlAdd.hideDialog();
      refresh();
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    const response = await apiService.update(
      "TrainGroupCategories",
      { ...dto, name: dto.name.trim() },
      dto.id
    );

    if (response) {
      dialogControlEdit.hideDialog();
      refresh();
    }
  };

  const onDelete = async (): Promise<void> => {
    await apiService.delete("TrainGroupCategories", dto.id);
    dialogControlDelete.hideDialog();
    refresh();
  };

  return (
    <>
      <Card title={t("Train group categories")}>
        <DataTableComponent
          controller="TrainGroupCategories"
          isUrlStateEnabled
          dataTableDto={datatableDto}
          setDataTableDto={setDatatableDto}
          formMode={FormMode.EDIT}
          onButtonClick={onDataTableClick}
          filterDisplay={DataTableFilterDisplayEnum.ROW}
          dataTableColumns={dataTableColumns}
          triggerRefreshData={triggerRefreshDataTable}
          availableGridRowButtons={availableGridRowButtons()}
        />
      </Card>

      <GenericDialogComponent
        header={t("Category")}
        visible={isViewDialogVisible}
        control={dialogControlView}
        formMode={FormMode.VIEW}
      >
        <TrainGroupCategoryFormComponent
          dto={dto}
          update={update}
        />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={t("New category")}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        formMode={FormMode.ADD}
        onSave={onSaveAdd}
      >
        <TrainGroupCategoryFormComponent
          dto={dto}
          update={update}
        />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={t("Category")}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        formMode={FormMode.EDIT}
        onSave={onSaveEdit}
      >
        <TrainGroupCategoryFormComponent
          dto={dto}
          update={update}
        />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={`${t("Are you sure")}?`}
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        formMode={FormMode.DELETE}
        onDelete={onDelete}
      >
        <div className="flex justify-content-center">
          <p className="m-0">
            {t("The train groups in this category will be left without one")}.
          </p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
