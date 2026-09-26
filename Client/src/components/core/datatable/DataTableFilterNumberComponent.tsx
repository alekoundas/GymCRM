import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { useEffect, useRef, useState } from "react";
import { InputText } from "primereact/inputtext";
import { OverlayPanel } from "primereact/overlaypanel";
import { InputNumber, InputNumberChangeEvent } from "primereact/inputnumber";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  options: ColumnFilterElementTemplateOptions;
}

export default function DataTableFilterNumberComponent({ options }: IField) {
  const { t } = useTranslator();
  const [value, setValue] = useState<number | undefined>();

  // Follows the grid's value: cleared, or restored from the url on coming back to
  // the page - where it would otherwise still filter the rows while showing nothing.
  useEffect(() => {
    const restored = Number(options.value);
    if (options.value === null || options.value === undefined || options.value === "")
      setValue(undefined);
    else if (!Number.isNaN(restored)) setValue(restored);
  }, [options.value]);

  const onChange = (e: InputNumberChangeEvent) => {
    setValue(e.value ?? undefined);
    options.filterApplyCallback(e.value?.toString());
  };

  return (
    <>
      <InputNumber
        value={value}
        // placeholder="Select Time Range"
        placeholder={t("Search")}
        onChange={(e) => onChange(e)}
      />
    </>
  );
}
