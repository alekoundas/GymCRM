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

  // Follows the grid's value: cleared, or a range restored from the url on coming
  // back to the page - where it would otherwise still filter the rows while showing
  // an empty box.
  useEffect(() => {
    if (Array.isArray(options.value) && options.value.length === 2) {
      const restored = options.value.map((x: string) => new Date(x));
      if (restored.every((x: Date) => !Number.isNaN(x.getTime()))) {
        setDates(restored);
        return;
      }
    }
    setDates([]);
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
