import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useMailStore } from "../../stores/MailStore";
import { useState } from "react";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import "react-quill-new/dist/quill.snow.css";
import "primeicons/primeicons.css";
import { InputText } from "primereact/inputtext";
import RichTextAreaComponent from "../../components/core/text-area/RichTextAreaComponent";

interface IField extends DialogChildProps {}

export default function MailFormComponent({ formMode }: IField) {
  const { mailDto, updateMailDto } = useMailStore();

  return (
    <div className="flex flex-column gap-3">
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
          Subject
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
          label="Body"
        />
      </div>
    </div>
  );
}
