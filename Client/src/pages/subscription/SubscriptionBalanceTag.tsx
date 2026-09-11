import { Tag } from "primereact/tag";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  balance: number | undefined;
  // Administrators see the real signed number. A member is shown how many are
  // missing instead, so no minus sign ever reaches them.
  isAdminView?: boolean;
}

export default function SubscriptionBalanceTag({ balance, isAdminView = true }: IField) {
  const { t } = useTranslator();
  const value = balance ?? 0;

  if (value > 0)
    return (
      <Tag
        severity="success"
        value={value.toString()}
      />
    );

  if (value === 0)
    return (
      <Tag
        severity="warning"
        value="0"
      />
    );

  return (
    <Tag
      severity="danger"
      value={isAdminView ? value.toString() : `${t("Missing")} ${Math.abs(value)}`}
    />
  );
}
