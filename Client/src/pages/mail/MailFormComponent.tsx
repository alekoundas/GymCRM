import { FormMode } from "../../enum/FormMode";
import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useMailStore } from "../../stores/MailStore";

interface IField extends DialogChildProps {}

export default function MailFormComponent({ formMode }: IField) {
  const { mailDto, updateMailDto } = useMailStore();

  return (
    <>
      <form>
        <div className="flex align-items-center justify-content-center">
          <div className="field">
            <label htmlFor="name">Role Name</label>
            <InputText
              id="name"
              name="name"
              value={mailDto.body}
              onChange={(x) =>
                updateMailDto({ [x.target.name]: x.target.value })
              }
              disabled={formMode !== FormMode.ADD}
            />
          </div>
        </div>
      </form>
    </>
  );
}
