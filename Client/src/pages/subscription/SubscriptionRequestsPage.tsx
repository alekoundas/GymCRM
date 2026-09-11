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
import SubscriptionDecideFormComponent from "./SubscriptionDecideFormComponent";
import { Button } from "primereact/button";

export default function SubscriptionRequestsPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const {
    subscriptionDto,
    setSubscriptionDto,
    subscriptionDecideDto,
    setSubscriptionDecideDto,
  } = useSubscriptionStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<SubscriptionDto>) => void) | undefined
  >(undefined);

  const [isDecideDialogVisible, setDecideDialogVisibility] = useState(false);

  const dialogControlDecide: DialogControl = {
    showDialog: () => setDecideDialogVisibility(true),
    hideDialog: () => setDecideDialogVisibility(false),
  };

  // Waiting only. Once decided a request leaves this page and turns up in the
  // history, so what is left here is always work still to do.
  const [datatableDto, setDatatableDto] = useState<DataTableDto<SubscriptionDto>>({
    ...new DataTableDto(),
    rows: 10,
    filters: [
      { fieldName: "status", filterType: "equals", value: SubscriptionStatusEnum.PENDING },
      { fieldName: "userId", filterType: "in" },
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
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => formatDate(rowData.createdOn),
      style: { width: "14%" },
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
      style: { width: "28%" },
    },
    {
      field: "requestedAmount",
      header: t("Requested"),
      sortable: true,
      filter: false,
      filterPlaceholder: "",
      style: { width: "12%" },
    },
    {
      field: "memberComment",
      header: t("Comment"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "34%" },
    },
  ];

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => [ButtonTypeEnum.EDIT];

  const onDataTableClick = (buttonType: ButtonTypeEnum, rowData?: SubscriptionDto) => {
    if (buttonType !== ButtonTypeEnum.EDIT || !rowData) return;

    setSubscriptionDto({ ...rowData });
    // Opens on what the member asked for, so approving as-is is one click.
    setSubscriptionDecideDto({
      id: rowData.id,
      isApproved: true,
      amount: rowData.requestedAmount ?? 0,
      adminComment: "",
      notifyUser: true,
    });
    dialogControlDecide.showDialog();
  };

  const decide = async (isApproved: boolean): Promise<void> => {
    const response = await apiService.decideSubscription({
      ...subscriptionDecideDto,
      isApproved,
    });

    if (response) {
      dialogControlDecide.hideDialog();
      if (triggerRefreshDataTable.current)
        triggerRefreshDataTable.current(datatableDto);
    }
  };

  return (
    <>
      <Card title={t("Subscription requests")}>
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
        header={t("Subscription request")}
        visible={isDecideDialogVisible}
        control={dialogControlDecide}
        formMode={FormMode.EDIT}
        footer={
          <div className="flex justify-content-between align-items-center">
            <Button
              label={t("Cancel")}
              icon="pi pi-times"
              text
              onClick={() => dialogControlDecide.hideDialog()}
            />
            <div className="flex gap-2">
              <Button
                label={t("Reject")}
                icon="pi pi-times"
                severity="danger"
                outlined
                onClick={() => decide(false)}
              />
              <Button
                label={t("Approve")}
                icon="pi pi-check"
                onClick={() => decide(true)}
              />
            </div>
          </div>
        }
      >
        <SubscriptionDecideFormComponent formMode={FormMode.EDIT} />
      </GenericDialogComponent>
    </>
  );
}
