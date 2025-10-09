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

  // Clear filter value where user presses clear.
  useEffect(() => {
    if (options.value === null) {
      setValue(undefined);
    }
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
