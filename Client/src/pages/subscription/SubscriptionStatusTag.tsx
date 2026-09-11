import { Tag } from "primereact/tag";
import { SubscriptionStatusEnum } from "../../enum/SubscriptionStatusEnum";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  status: SubscriptionStatusEnum | undefined;
}

export default function SubscriptionStatusTag({ status }: IField) {
  const { t } = useTranslator();

  if (status === SubscriptionStatusEnum.APPROVED)
    return (
      <Tag
        severity="success"
        icon="pi pi-check"
        value={t("Approved")}
      />
    );

  if (status === SubscriptionStatusEnum.REJECTED)
    return (
      <Tag
        severity="danger"
        icon="pi pi-times"
        value={t("Rejected")}
      />
    );

  return (
    <Tag
      severity="warning"
      icon="pi pi-clock"
      value={t("Pending")}
    />
  );
}
