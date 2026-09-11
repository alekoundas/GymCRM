import { InputNumber } from "primereact/inputnumber";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";
import { Checkbox } from "primereact/checkbox";
import { Message } from "primereact/message";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useSubscriptionStore } from "../../stores/SubscriptionStore";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function SubscriptionDecideFormComponent({}: IField) {
  const { t } = useTranslator();
  const { subscriptionDto, subscriptionDecideDto, updateSubscriptionDecideDto } =
    useSubscriptionStore();

  const memberName = subscriptionDto.user
    ? `${subscriptionDto.user.firstName ?? ""} ${subscriptionDto.user.lastName ?? ""}`.trim()
    : "";

  return (
    <div className="flex flex-column gap-3">
      <div className="field">
        <label className="block text-900 font-medium mb-2">{t("User")}</label>
        <InputText
          value={memberName}
          disabled
          className="w-full"
        />
      </div>

      <div className="formgrid grid">
        <div className="field col-12 md:col-6">
          <label className="block text-900 font-medium mb-2">{t("Requested")}</label>
          <InputNumber
            value={subscriptionDto.requestedAmount ?? null}
            disabled
            className="w-full"
          />
        </div>

        <div className="field col-12 md:col-6">
          <label
            htmlFor="decide-amount"
            className="block text-900 font-medium mb-2"
          >
            {t("Granted")} *
          </label>
          <InputNumber
            inputId="decide-amount"
            value={subscriptionDecideDto.amount || null}
            onValueChange={(e) => updateSubscriptionDecideDto({ amount: e.value ?? 0 })}
            min={1}
            max={999}
            showButtons
            className="w-full"
          />
        </div>
      </div>

      {(subscriptionDto.memberComment?.length ?? 0) > 0 && (
        <Message
          severity="info"
          className="w-full justify-content-start"
          text={subscriptionDto.memberComment}
        />
      )}

      <div className="field">
        <label
          htmlFor="decide-comment"
          className="block text-900 font-medium mb-2"
        >
          {t("Comment")}
        </label>
        <InputTextarea
          id="decide-comment"
          rows={3}
          value={subscriptionDecideDto.adminComment}
          onChange={(e) => updateSubscriptionDecideDto({ adminComment: e.target.value })}
          className="w-full"
        />
      </div>

      <div className="field flex align-items-center gap-2">
        <Checkbox
          inputId="decide-notify"
          checked={subscriptionDecideDto.notifyUser}
          onChange={(e) => updateSubscriptionDecideDto({ notifyUser: e.checked ?? false })}
        />
        <label
          htmlFor="decide-notify"
          className="text-900 font-medium"
        >
          {t("Notify user")}
        </label>
      </div>
    </div>
  );
}
