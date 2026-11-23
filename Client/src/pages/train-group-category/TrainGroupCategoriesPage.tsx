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
import { CategoryDotComponent } from "../../components/core/category/CategoryTagComponent";
import { LookupDto } from "../../model/lookup/LookupDto";
import {
  categoryColorOf,
  nextFreeCategoryColor,
} from "../../services/CategoryColorService";

// The categories train groups can be put in. Each one becomes a tab on the admin
// calendar and a filter on the booking page, in its own colour.
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
      style: { width: "75%" },
    },
    {
      field: "color",
      header: t("Color"),
      body: (rowData: TrainGroupCategoryDto) => {
        const color = categoryColorOf(rowData);
        return (
          <span className="flex align-items-center gap-2">
            <CategoryDotComponent
              color={color}
              size="1rem"
            />
            <span className="text-sm text-color-secondary">{color}</span>
          </span>
        );
      },
      style: { width: "25%" },
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
        openAdd();
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

  // A new category starts on a colour none of the others has, so two of them are not
  // told apart by name alone unless the admin wants it that way.
  const openAdd = async () => {
    const lookupDto = new LookupDto();
    lookupDto.take = 1000;
    const response = await apiService.getDataLookup("TrainGroupCategories", lookupDto);
    const used = (response?.data ?? []).map((x) =>
      categoryColorOf({ id: Number(x.id), color: x.color })
    );

    setDto({ ...new TrainGroupCategoryDto(), color: nextFreeCategoryColor(used) });
    dialogControlAdd.showDialog();
  };

  const onSaveAdd = async (): Promise<void> => {
    const response = await apiService.create("TrainGroupCategories", {
      name: dto.name.trim(),
      color: categoryColorOf(dto),
    });

    if (response) {
      dialogControlAdd.hideDialog();
      refresh();
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    const response = await apiService.update(
      "TrainGroupCategories",
      // A category made before colours is saved with the preset it was showing.
      { ...dto, name: dto.name.trim(), color: categoryColorOf(dto) },
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
