import { FormMode } from "../../enum/FormMode";
import { InputText } from "primereact/inputtext";
import { useRoleStore } from "../../stores/RoleStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function RoleFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const { roleDto, updateRoleDto } = useRoleStore();

  return (
    <>
      <form>
        <div className="flex align-items-center justify-content-center">
          <div className="field">
            <label htmlFor="name">{t("Role name")}</label>
            <InputText
              id="name"
              name="name"
              value={roleDto.name}
              onChange={(x) =>
                updateRoleDto({ [x.target.name]: x.target.value })
              }
              disabled={formMode !== FormMode.ADD}
            />
          </div>
        </div>
      </form>
    </>
  );
}
