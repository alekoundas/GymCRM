import { InputNumber } from "primereact/inputnumber";
import { InputTextarea } from "primereact/inputtextarea";
import { Checkbox } from "primereact/checkbox";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { useSubscriptionStore } from "../../stores/SubscriptionStore";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function SubscriptionAddFormComponent({}: IField) {
  const { t } = useTranslator();
  const { subscriptionAddDto, updateSubscriptionAddDto } = useSubscriptionStore();

  return (
    <div className="flex flex-column gap-3">
      <div className="field">
        <label
          htmlFor="subscription-user"
          className="block text-900 font-medium mb-2"
        >
          {t("User")}
        </label>
        <LookupComponent
          controller="users"
          selectedEntityId={subscriptionAddDto.userId}
          isEnabled={true}
          onChange={(e) => updateSubscriptionAddDto({ userId: e?.id ?? "" })}
        />
      </div>

      <div className="field">
        <label
          htmlFor="subscription-amount"
          className="block text-900 font-medium mb-2"
        >
          {t("Subscriptions")} *
        </label>
        <InputNumber
          inputId="subscription-amount"
          value={subscriptionAddDto.amount || null}
          onValueChange={(e) => updateSubscriptionAddDto({ amount: e.value ?? 0 })}
          min={1}
          max={999}
          showButtons
          className="w-full"
        />
      </div>

      <div className="field">
        <label
          htmlFor="subscription-comment"
          className="block text-900 font-medium mb-2"
        >
          {t("Comment")}
        </label>
        <InputTextarea
          id="subscription-comment"
          rows={3}
          value={subscriptionAddDto.adminComment}
          onChange={(e) => updateSubscriptionAddDto({ adminComment: e.target.value })}
          className="w-full"
        />
      </div>

      <div className="field flex align-items-center gap-2">
        <Checkbox
          inputId="subscription-notify"
          checked={subscriptionAddDto.notifyUser}
          onChange={(e) => updateSubscriptionAddDto({ notifyUser: e.checked ?? false })}
        />
        <label
          htmlFor="subscription-notify"
          className="text-900 font-medium"
        >
          {t("Notify user")}
        </label>
      </div>
    </div>
  );
}
