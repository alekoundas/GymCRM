import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { FormMode } from "../../enum/FormMode";
import { TrainGroupCategoryDto } from "../../model/entities/train-group-category/TrainGroupCategoryDto";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {
  dto: TrainGroupCategoryDto;
  update: (updates: Partial<TrainGroupCategoryDto>) => void;
}

export default function TrainGroupCategoryFormComponent({
  formMode,
  dto,
  update,
}: IField) {
  const { t } = useTranslator();

  return (
    <div className="field">
      <label
        htmlFor="category-name"
        className="block text-900 font-medium mb-2"
      >
        {t("Category")}
      </label>
      <InputText
        id="category-name"
        value={dto.name}
        onChange={(e) => update({ name: e.target.value })}
        placeholder={t("Category")}
        maxLength={100}
        disabled={formMode === FormMode.VIEW}
        className="w-full"
      />
    </div>
  );
}
