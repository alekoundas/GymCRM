import { TokenService } from "../../services/TokenService";
import { useRef, useState } from "react";
import { Card } from "primereact/card";
import { Avatar } from "primereact/avatar";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { SubscriptionDto } from "../../model/entities/subscription/SubscriptionDto";
import { SubscriptionStatusEnum } from "../../enum/SubscriptionStatusEnum";
import { useSubscriptionStore } from "../../stores/SubscriptionStore";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { UserDto } from "../../model/entities/user/UserDto";
import SubscriptionStatusTag from "./SubscriptionStatusTag";
import SubscriptionAddFormComponent from "./SubscriptionAddFormComponent";
import { Button } from "primereact/button";

export default function SubscriptionsPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const {
    subscriptionDto,
    setSubscriptionDto,
    subscriptionAddDto,
    resetSubscriptionAddDto,
  } = useSubscriptionStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<SubscriptionDto>) => void) | undefined
  >(undefined);

  const [isAddDialogVisible, setAddDialogVisibility] = useState(false);
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false);

  const dialogControlAdd: DialogControl = {
    showDialog: () => {
      resetSubscriptionAddDto();
      setAddDialogVisibility(true);
    },
    hideDialog: () => setAddDialogVisibility(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  // Decided entries only. Anything still waiting lives on the requests page, so
  // this one reads as a ledger rather than a queue.
  const [datatableDto, setDatatableDto] = useState<DataTableDto<SubscriptionDto>>({
    ...new DataTableDto(),
    rows: 10,
    filters: [
      { fieldName: "userId", filterType: "in" },
      { fieldName: "createdOn", filterType: "between" },
    ],
    dataTableSorts: [{ field: "createdOn", order: -1 }],
  });

  // Same face-and-name treatment the other grids use for a participant.
  const memberTemplate = (user: UserDto | undefined) => {
    if (!user) return <></>;

    const initials = `${user.firstName?.charAt(0) ?? ""}${user.lastName?.charAt(0) ?? ""}`.toUpperCase();

    return (
      <div className="flex m-0 p-0 align-items-center">
        <Avatar
          image={user.profileImage ? "data:image/png;base64," + user.profileImage : ""}
          label={user.profileImage ? undefined : initials}
          shape="circle"
          size="normal"
          className="mr-2"
        />
        {user.firstName} {user.lastName}
      </div>
    );
  };

  const formatDate = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  const dataTableColumns: DataTableColumns<SubscriptionDto>[] = [
    {
      field: "createdOn",
      header: t("Date"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => <DataTableFilterDateComponent options={options} />,
      body: (rowData) => formatDate(rowData.createdOn),
      style: { width: "12%" },
    },
    {
      field: "userId",
      header: t("User"),
      sortable: false,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData) => memberTemplate(rowData.user),
      style: { width: "24%" },
    },
    {
      field: "requestedAmount",
      header: t("Requested"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      // A dash, not a zero: nobody asked for this one, the trainer added it.
      body: (rowData) => rowData.requestedAmount ?? "—",
      style: { width: "10%" },
    },
    {
      field: "amount",
      header: t("Granted"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "10%" },
    },
    {
      field: "status",
      header: t("Status"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => <SubscriptionStatusTag status={rowData.status} />,
      style: { width: "12%" },
    },
    {
      field: "adminComment",
      header: t("Comment"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "22%" },
    },
  ];

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    const result: ButtonTypeEnum[] = [];

    // ADD is what puts the button in the grid header, not on a row.
    if (TokenService.isUserAllowed("SubscriptionsAdmin_Add"))
      result.push(ButtonTypeEnum.ADD);

    if (TokenService.isUserAllowed("SubscriptionsAdmin_Delete"))
      result.push(ButtonTypeEnum.DELETE);

    return result;
  };

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: SubscriptionDto) => {
    if (rowData) setSubscriptionDto({ ...rowData });

    switch (buttonType) {
      case ButtonTypeEnum.ADD:
        dialogControlAdd.showDialog();
        break;
      case ButtonTypeEnum.DELETE:
        dialogControlDelete.showDialog();
        break;
      default:
        break;
    }
  };

  const refresh = () => {
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);
  };

  const onSaveAdd = async (): Promise<void> => {
    const response = await apiService.addSubscription(subscriptionAddDto);
    if (response) {
      dialogControlAdd.hideDialog();
      refresh();
    }
  };

  // Removing an entry moves somebody's balance, so it asks whether to say so.
  const onDelete = async (notify: boolean): Promise<void> => {
    const response = await apiService.removeSubscription(subscriptionDto.id, notify);
    if (response !== null) {
      dialogControlDelete.hideDialog();
      refresh();
    }
  };

  return (
    <>
      <Card title={t("Subscriptions")}>
        <DataTableComponent
          controller="Subscriptions"
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
        header={t("New subscription")}
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={onSaveAdd}
        formMode={FormMode.ADD}
      >
        <SubscriptionAddFormComponent formMode={FormMode.ADD} />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={t("Delete subscription")}
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        formMode={FormMode.VIEW}
        footer={
          <div className="flex justify-content-between align-items-center">
            <Button
              label={t("Cancel")}
              icon="pi pi-times"
              text
              onClick={() => dialogControlDelete.hideDialog()}
            />
            <div className="flex gap-2">
              <Button
                label={t("Delete")}
                icon="pi pi-trash"
                severity="danger"
                outlined
                onClick={() => onDelete(false)}
              />
              <Button
                label={t("Delete and notify")}
                icon="pi pi-send"
                severity="danger"
                onClick={() => onDelete(true)}
              />
            </div>
          </div>
        }
      >
        <div>
          <p className="m-0">{t("This will change the remaining subscriptions")}.</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
