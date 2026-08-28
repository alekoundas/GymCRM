import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useMailStore } from "../../stores/MailStore";
import { useState } from "react";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import "react-quill-new/dist/quill.snow.css";
import "primeicons/primeicons.css";
import { InputText } from "primereact/inputtext";
import RichTextAreaComponent from "../../components/core/text-area/RichTextAreaComponent";
import { useTranslator } from "../../services/TranslatorService";
import { MailStatusEnum } from "../../enum/MailStatusEnum";
import { Message } from "primereact/message";
import { Tag } from "primereact/tag";

interface IField extends DialogChildProps {}

export default function MailFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const { mailDto, updateMailDto } = useMailStore();

  const formatDateTime = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()} ${date
      .getHours()
      .toString()
      .padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")}`;
  };

  return (
    <div className="flex flex-column gap-3">
      {formMode === FormMode.VIEW && (
        <div className="flex flex-column gap-2">
          <div className="flex align-items-center gap-2">
            {mailDto.status === MailStatusEnum.SENT ? (
              <Tag
                severity="success"
                icon="pi pi-check"
                value={t("Sent")}
              />
            ) : mailDto.status === MailStatusEnum.FAILED ? (
              <Tag
                severity="danger"
                icon="pi pi-times"
                value={t("Failed")}
              />
            ) : (
              <Tag
                severity="warning"
                icon="pi pi-clock"
                value={t("Pending")}
              />
            )}
            {mailDto.sentOn && (
              <span className="text-sm text-color-secondary">
                {formatDateTime(mailDto.sentOn)}
              </span>
            )}
          </div>

          {(mailDto.error?.length ?? 0) > 0 && (
            <Message
              severity="error"
              className="w-full justify-content-start"
              text={mailDto.error}
            />
          )}
        </div>
      )}

      <div className="field">
        <label
          htmlFor="userId"
          className="block text-900 font-medium mb-2"
        >
          Recipients
        </label>
        <LookupComponent
          controller="users"
          selectedEntityId={mailDto.userId ?? ""}
          isEnabled={formMode !== FormMode.VIEW}
          onChange={(e) => updateMailDto({ userId: e?.id })}
        />
      </div>
      <div className="field">
        <label
          htmlFor="subject"
          className="block text-900 font-medium mb-2"
        >
          {t("Subject")}
        </label>
        <InputText
          id="subject"
          name="subject"
          value={mailDto.subject}
          onChange={(e) => updateMailDto({ [e.target.name]: e.target.value })}
          disabled={formMode === FormMode.VIEW}
          className="w-full"
        />
      </div>
      <div className="field">
        <RichTextAreaComponent
          value={mailDto.body}
          onChange={(e) => updateMailDto({ body: e })}
          isEnabled={formMode !== FormMode.VIEW}
          label={t("Body")}
        />
      </div>
    </div>
  );
}
