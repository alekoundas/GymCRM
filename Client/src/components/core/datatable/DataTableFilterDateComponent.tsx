import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { FormEvent } from "primereact/ts-helpers";
import { SyntheticEvent, useEffect, useState } from "react";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  options: ColumnFilterElementTemplateOptions;
}

export default function DataTableFilterDateComponent({ options }: IField) {
  const { t } = useTranslator();
  const [dates, setDates] = useState<Date[]>([]);

  // Clear filter value where user presses clear.
  useEffect(() => {
    if (options.value === null) setDates([]);
  }, [options.value]);

  const onHide = () => {
    const result: string[] = [];

    if (dates.every((x) => x !== null) && dates.length === 2) {
      const start = new Date(dates[0]);
      start.setHours(0, 0, 0, 0);
      const end = new Date(dates[1]);
      end.setHours(23, 59, 59, 999);

      result.push(start.toISOString());

      result.push(end.toISOString());
      options.filterApplyCallback(result);
    } else {
      setDates([]);
    }
  };

  return (
    <Calendar
      value={dates}
      onChange={(e) => setDates(e.value as [])}
      placeholder={t("Search")}
      selectionMode="range"
      readOnlyInput
      hideOnRangeSelection
      onHide={onHide}
    />
  );
}
