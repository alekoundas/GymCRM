import { Card } from "primereact/card";
import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { PhoneNumberDto } from "../../model/entities/phone-number/PhoneNumberDto";
import PhoneNumberFormComponent from "./PhoneNumberFormComponent";
import { usePhoneNumberStore } from "../../stores/PhoneNumberStore";
import { InputSwitch } from "primereact/inputswitch";
import { TokenService } from "../../services/TokenService";
import { ColumnBodyOptions } from "primereact/column";
import { Tag } from "primereact/tag";

export default function PhoneNumberGridComponent() {
  const { phoneNumberDto, setPhoneNumberDto, resetPhoneNumberDto } =
    usePhoneNumberStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<PhoneNumberDto>) => void) | undefined
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
  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<PhoneNumberDto>
  >({
    ...new DataTableDto(),
    filters: [
      {
        fieldName: "UserId",
        value: TokenService.getUserId(),
        filterType: "equals",
      },
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

  const dataTableColumns: DataTableColumns<PhoneNumberDto>[] = [
    {
      field: "number",
      header: "Number",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "40%" },
    },
    {
      field: "isPrimary",
      header: "Primary",
      sortable: false,
      filter: false,
      filterPlaceholder: "Search",
      style: { width: "20%" },
      body: (rowData: PhoneNumberDto, options: ColumnBodyOptions) => {
        if (rowData.isPrimary)
          return (
            <Tag
              className="p-2"
              severity={"secondary"}
            >
              Primary
            </Tag>
          );
      },
    },
  ];

  const OnSaveAdd = async (): Promise<void> => {
    const response = await ApiService.create("PhoneNumbers", phoneNumberDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetPhoneNumberDto();

      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const OnSaveEdit = async (): Promise<void> => {
    if (phoneNumberDto.id) {
      const response = await ApiService.update(
        "PhoneNumbers",
        phoneNumberDto,
        phoneNumberDto.id
      );

      if (response) {
        dialogControlEdit.hideDialog();
        setPhoneNumberDto(new PhoneNumberDto());

        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      }
    }
  };

  const onDelete = async (): Promise<void> => {
    if (phoneNumberDto.id) {
      const response = await ApiService.delete(
        "PhoneNumbers",
        phoneNumberDto.id
      );

      dialogControlDelete.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: PhoneNumberDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setPhoneNumberDto(rowData);
          dialogControlView.showDialog();
        }
        break;
      case ButtonTypeEnum.ADD:
        resetPhoneNumberDto();
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setPhoneNumberDto(rowData);
          dialogControlEdit.showDialog();
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setPhoneNumberDto(rowData);
          dialogControlDelete.showDialog();
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.EDIT}
        onButtonClick={onDataTableClick}
        controller="PhoneNumbers"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />

      {/*                                      */}
      {/*           View Train Group           */}
      {/*                                      */}

      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewDialogVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <PhoneNumberFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        formMode={FormMode.ADD}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <PhoneNumberFormComponent />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        formMode={FormMode.EDIT}
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <PhoneNumberFormComponent />
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
