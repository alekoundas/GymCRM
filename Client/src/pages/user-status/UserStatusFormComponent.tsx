import { InputText } from "primereact/inputtext";
import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useUserStatusStore } from "../../stores/UserStatusStore";
import { useTranslator } from "../../services/TranslatorService";
import { ColorPicker } from "primereact/colorpicker";

interface IField extends DialogChildProps {}

export default function UserStatusFormComponent({ formMode }: IField) {
  const { t } = useTranslator();
  const { userStatusDto, updateUserStatusDto } = useUserStatusStore();

  return (
    <div className="flex flex-column md:flex-row justify-content-center ">
      <div className="field pr-8">
        <label
          htmlFor="name"
          className="block text-900 font-medium mb-2"
        >
          {t("Name")}
        </label>
        <InputText
          id="name"
          type="text"
          placeholder={t("Name")}
          value={userStatusDto.name}
          onChange={(x) => updateUserStatusDto({ name: x.target.value })}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      <div className="field ">
        <label
          htmlFor="color"
          className="block text-900 font-medium mb-2"
        >
          {t("Color")}
        </label>
        <InputText
          id="color"
          type="text"
          placeholder={t("Color")}
          value={userStatusDto.color}
          disabled={true}
        />
        <ColorPicker
          id="color"
          type="text"
          placeholder={t("Color")}
          format="hex"
          value={userStatusDto.color}
          onChange={(x) => updateUserStatusDto({ color: x.value?.toString() })}
          disabled={formMode === FormMode.VIEW}
        />
      </div>
    </div>
  );
}
