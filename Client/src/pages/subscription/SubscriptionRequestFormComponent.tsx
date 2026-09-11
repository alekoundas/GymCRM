import { InputNumber } from "primereact/inputnumber";
import { InputTextarea } from "primereact/inputtextarea";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {
  requestedAmount: number;
  setRequestedAmount: (value: number) => void;
  memberComment: string;
  setMemberComment: (value: string) => void;
}

export default function SubscriptionRequestFormComponent({
  requestedAmount,
  setRequestedAmount,
  memberComment,
  setMemberComment,
}: IField) {
  const { t } = useTranslator();

  return (
    <div className="flex flex-column gap-3">
      <p className="m-0 text-color-secondary">
        {t("Your trainer will review this and get back to you")}.
      </p>

      <div className="field">
        <label
          htmlFor="request-amount"
          className="block text-900 font-medium mb-2"
        >
          {t("How many lessons")} *
        </label>
        <InputNumber
          inputId="request-amount"
          value={requestedAmount || null}
          onValueChange={(e) => setRequestedAmount(e.value ?? 0)}
          min={1}
          max={999}
          showButtons
          className="w-full"
        />
      </div>

      <div className="field">
        <label
          htmlFor="request-comment"
          className="block text-900 font-medium mb-2"
        >
          {t("Comment")}
        </label>
        <InputTextarea
          id="request-comment"
          rows={3}
          value={memberComment}
          onChange={(e) => setMemberComment(e.target.value)}
          className="w-full"
        />
      </div>
    </div>
  );
}
