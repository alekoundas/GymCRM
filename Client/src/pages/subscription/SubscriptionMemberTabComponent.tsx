import { useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { FormMode } from "../../enum/FormMode";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { SubscriptionDto } from "../../model/entities/subscription/SubscriptionDto";
import { SubscriptionStatusEnum } from "../../enum/SubscriptionStatusEnum";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";
import SubscriptionStatusTag from "./SubscriptionStatusTag";
import SubscriptionBalanceTag from "./SubscriptionBalanceTag";
import SubscriptionRequestFormComponent from "./SubscriptionRequestFormComponent";

interface IField {
  userId: string;
  // True when an administrator is looking at somebody else's profile. They get the
  // signed balance and no request button - asking is the member's own business.
  isAdminView: boolean;
  // Bumped by the parent so the balance is re-read when something outside this tab
  // changed it.
  refreshToken?: number;
}

export default function SubscriptionMemberTabComponent({
  userId,
  isAdminView,
  refreshToken,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<SubscriptionDto>) => void) | undefined
  >(undefined);

  const [balance, setBalance] = useState<number | null>(null);
  const [pending, setPending] = useState<SubscriptionDto | undefined>(undefined);

  const [isRequestDialogVisible, setRequestDialogVisibility] = useState(false);
  const [isCancelDialogVisible, setCancelDialogVisibility] = useState(false);
  const [requestedAmount, setRequestedAmount] = useState(0);
  const [memberComment, setMemberComment] = useState("");

  const dialogControlRequest: DialogControl = {
    showDialog: () => {
      setRequestedAmount(0);
      setMemberComment("");
      setRequestDialogVisibility(true);
    },
    hideDialog: () => setRequestDialogVisibility(false),
  };

  const dialogControlCancel: DialogControl = {
    showDialog: () => setCancelDialogVisibility(true),
    hideDialog: () => setCancelDialogVisibility(false),
  };

  const [datatableDto, setDatatableDto] = useState<DataTableDto<SubscriptionDto>>({
    ...new DataTableDto(),
    rows: 10,
    // Capital U: the server converts this one to a Guid by name.
    filters: [{ fieldName: "UserId", value: userId, filterType: "equals" }],
    dataTableSorts: [{ field: "createdOn", order: -1 }],
  });

  const loadBalance = async () => {
    const response = await apiService.getSubscriptionBalance(
      isAdminView ? userId : undefined,
    );
    if (response !== null) setBalance(response);
  };

  useEffect(() => {
    loadBalance();
  }, [userId, refreshToken]);

  // The rows come back through the grid, so the pending one is picked out of what
  // is already on screen rather than asked for a second time.
  useEffect(() => {
    setPending(
      datatableDto.data.find((x) => x.status === SubscriptionStatusEnum.PENDING),
    );
  }, [datatableDto.data]);

  const refresh = () => {
    if (triggerRefreshDataTable.current)
      triggerRefreshDataTable.current(datatableDto);

    loadBalance();
  };

  const onRequest = async (): Promise<void> => {
    const response = await apiService.requestSubscription(requestedAmount, memberComment);
    if (response) {
      dialogControlRequest.hideDialog();
      refresh();
    }
  };

  const onCancelRequest = async (): Promise<void> => {
    if (!pending) return;

    const response = await apiService.delete<SubscriptionDto>("Subscriptions", pending.id);
    if (response !== null) {
      dialogControlCancel.hideDialog();
      refresh();
    }
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
      style: { width: "18%" },
    },
    {
      field: "requestedAmount",
      header: t("Requested"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => rowData.requestedAmount ?? "—",
      style: { width: "16%" },
    },
    {
      field: "amount",
      header: t("Granted"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "16%" },
    },
    {
      field: "status",
      header: t("Status"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      body: (rowData) => <SubscriptionStatusTag status={rowData.status} />,
      style: { width: "18%" },
    },
    {
      field: "adminComment",
      header: t("Comment"),
      sortable: false,
      filter: false,
      filterPlaceholder: "",
      style: { width: "32%" },
    },
  ];

  const canRequest =
    !isAdminView && TokenService.isUserAllowed("Subscriptions_Add") && !pending;

  const canCancel =
    !isAdminView && TokenService.isUserAllowed("Subscriptions_Delete") && !!pending;

  return (
    <>
      <div className="flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <div className="flex align-items-center gap-2">
          <span className="text-color-secondary">{t("Remaining lessons")}:</span>
          <SubscriptionBalanceTag
            balance={balance ?? 0}
            isAdminView={isAdminView}
          />
        </div>

        <div className="flex gap-2">
          {canRequest && (
            <Button
              label={t("Ask for more lessons")}
              icon="pi pi-plus"
              onClick={() => dialogControlRequest.showDialog()}
            />
          )}
          {canCancel && (
            <Button
              label={t("Cancel request")}
              icon="pi pi-times"
              outlined
              severity="danger"
              onClick={() => dialogControlCancel.showDialog()}
            />
          )}
        </div>
      </div>

      {pending && (
        <Message
          severity="info"
          className="w-full justify-content-start mb-3"
          text={`${t("You have asked for")} ${pending.requestedAmount} ${t(
            "lessons and are waiting for an answer",
          )}.`}
        />
      )}

      <DataTableComponent
        controller="Subscriptions"
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.VIEW}
        onButtonClick={() => {}}
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={[] as ButtonTypeEnum[]}
      />

      <GenericDialogComponent
        header={t("Ask for more lessons")}
        visible={isRequestDialogVisible}
        control={dialogControlRequest}
        onSave={onRequest}
        formMode={FormMode.ADD}
      >
        <SubscriptionRequestFormComponent
          formMode={FormMode.ADD}
          requestedAmount={requestedAmount}
          setRequestedAmount={setRequestedAmount}
          memberComment={memberComment}
          setMemberComment={setMemberComment}
        />
      </GenericDialogComponent>

      <GenericDialogComponent
        header={t("Cancel request")}
        visible={isCancelDialogVisible}
        control={dialogControlCancel}
        formMode={FormMode.VIEW}
        footer={
          <div className="flex justify-content-between align-items-center">
            <Button
              label={t("Close")}
              icon="pi pi-times"
              text
              onClick={() => dialogControlCancel.hideDialog()}
            />
            <Button
              label={t("Cancel request")}
              icon="pi pi-times"
              severity="danger"
              onClick={onCancelRequest}
            />
          </div>
        }
      >
        <div>
          <p className="m-0">{t("Your request will be withdrawn")}.</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
