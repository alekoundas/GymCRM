import { FormMode } from "../../enum/FormMode";
import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import AutoCompleteComponent from "../../components/core/auto-complete/AutoCompleteComponent";
import UserChipComponent from "../../components/core/user/UserChipComponent";
import RichTextAreaComponent from "../../components/core/text-area/RichTextAreaComponent";
import { useMailStore } from "../../stores/MailStore";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function MailSendFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const { mailSendDto, updateMailSendDto } = useMailStore();

  // A suggestion: the member as everywhere else, with the address the email will go
  // to beside it, since that is what this box is for. One line - the list's rows
  // have a fixed height, and a second line was cut off.
  const userTemplate = (user: UserDto) => (
    <div className="flex align-items-center justify-content-between gap-3 w-full">
      <UserChipComponent user={user} className="flex-none" />
      {user.email && (
        <small className="min-w-0 text-color-secondary white-space-nowrap overflow-hidden text-overflow-ellipsis">
          {user.email}
        </small>
      )}
    </div>
  );

  // A chosen recipient.
  const chipTemplate = (user: UserDto) => <UserChipComponent user={user} />;

  return (
    <div className="flex flex-column gap-3">
      <div className="field ">
        <label htmlFor="recipients">{t("Recipients")}</label>
        <AutoCompleteComponent
          controller="users"
          isEnabled={true}
          isSelectAllVisible
          itemTemplate={userTemplate}
          selectedItemTemplate={chipTemplate}
          onChange={(x) => updateMailSendDto({ userIds: x.map((y) => y.id) })}
        />
      </div>
      <div className="field">
        <label htmlFor="subject">{t("Subject")}</label>
        <InputText
          id="subject"
          name="subject"
          value={mailSendDto.subject}
          onChange={(e) => updateMailSendDto({ subject: e.target.value })}
          disabled={formMode === FormMode.VIEW}
          className="w-full"
        />
      </div>
      <div className="field">
        <RichTextAreaComponent
          value={mailSendDto.body}
          onChange={(e) => updateMailSendDto({ body: e })}
          isEnabled={formMode !== FormMode.VIEW}
          label={t("Body")}
        />
      </div>
    </div>
  );
}
