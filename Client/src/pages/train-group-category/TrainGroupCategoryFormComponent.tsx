import { Button } from "primereact/button";
import { ColorPicker } from "primereact/colorpicker";
import { InputText } from "primereact/inputtext";
import CategoryTagComponent from "../../components/core/category/CategoryTagComponent";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { FormMode } from "../../enum/FormMode";
import { TrainGroupCategoryDto } from "../../model/entities/train-group-category/TrainGroupCategoryDto";
import {
  CATEGORY_COLOR_PRESETS,
  categoryColorOf,
} from "../../services/CategoryColorService";
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
  const isViewMode = formMode === FormMode.VIEW;

  // What the category shows as now - for one made before colours, its preset.
  const color = categoryColorOf(dto);
  const isCustom = !CATEGORY_COLOR_PRESETS.includes(color);

  // A ring in the colour, apart from the swatch, marks the chosen one.
  const ring = (x: string) => `0 0 0 2px var(--surface-overlay), 0 0 0 4px ${x}`;

  return (
    <div className="flex flex-column gap-4">
      <div className="field m-0">
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
          disabled={isViewMode}
          className="w-full"
        />
      </div>

      <div className="field m-0">
        <span className="block text-900 font-medium mb-1">{t("Color")}</span>
        {!isViewMode && (
          <small className="block text-color-secondary mb-3">
            {t("Pick a color or choose your own")}
          </small>
        )}

        <div
          className="flex flex-wrap align-items-center gap-3"
          role="radiogroup"
          aria-label={t("Color")}
        >
          {CATEGORY_COLOR_PRESETS.map((x) => {
            const isSelected = x === color;
            return (
              <Button
                key={x}
                type="button"
                rounded
                role="radio"
                aria-checked={isSelected}
                aria-label={x}
                tooltip={x}
                tooltipOptions={{ position: "top", showDelay: 400 }}
                icon={isSelected ? "pi pi-check" : undefined}
                disabled={isViewMode}
                onClick={() => update({ color: x })}
                className="p-0 flex-none"
                style={{
                  width: "2.25rem",
                  height: "2.25rem",
                  background: x,
                  borderColor: x,
                  color: "#ffffff",
                  boxShadow: isSelected ? ring(x) : undefined,
                }}
              />
            );
          })}

          {/* Anything else. A rainbow until a colour of their own is picked, so it
              does not look like a second copy of the chosen preset. The picker works
              without the "#"; the category keeps it. */}
          <ColorPicker
            format="hex"
            value={color.slice(1)}
            onChange={(e) => {
              if (typeof e.value === "string") update({ color: `#${e.value}` });
            }}
            disabled={isViewMode}
            tooltip={t("Custom color")}
            tooltipOptions={{ position: "top", showDelay: 400 }}
            inputStyle={{
              width: "2.25rem",
              height: "2.25rem",
              borderRadius: "50%",
              backgroundImage: isCustom
                ? "none"
                : "conic-gradient(#ef4444, #eab308, #22c55e, #06b6d4, #3b82f6, #a855f7, #ef4444)",
              boxShadow: isCustom ? ring(color) : undefined,
            }}
            pt={{ input: { "aria-label": t("Custom color") } }}
          />
          <span className="text-sm text-color-secondary font-medium">{color}</span>
        </div>
      </div>

      <div className="flex flex-wrap align-items-center gap-2">
        <span className="text-sm text-color-secondary">
          {t("How it shows on the calendar")}:
        </span>
        <CategoryTagComponent
          name={dto.name.trim() || t("Category")}
          color={color}
        />
      </div>
    </div>
  );
}
