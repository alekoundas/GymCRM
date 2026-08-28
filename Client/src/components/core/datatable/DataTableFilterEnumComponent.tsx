import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { useEffect, useState } from "react";
import { Dropdown, DropdownChangeEvent } from "primereact/dropdown";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  options: ColumnFilterElementTemplateOptions;
  // Value is what the API filters on, label is what the column shows.
  values: { label: string; value: string }[];
}

export default function DataTableFilterEnumComponent({
  options,
  values,
}: IField) {
  const { t } = useTranslator();
  const [value, setValue] = useState<string | undefined>();

  // Clear filter value where user presses clear.
  useEffect(() => {
    if (options.value === null) {
      setValue(undefined);
    }
  }, [options.value]);

  const onChange = (e: DropdownChangeEvent) => {
    setValue(e.value ?? undefined);
    options.filterApplyCallback(e.value ?? undefined);
  };

  return (
    <Dropdown
      value={value}
      options={values}
      placeholder={t("Search")}
      onChange={(e) => onChange(e)}
      className="w-full"
      showClear
    />
  );
}
