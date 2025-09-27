import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { FormEvent } from "primereact/ts-helpers";
import { SyntheticEvent, useEffect, useState } from "react";

interface IField {
  options: ColumnFilterElementTemplateOptions;
}

export default function DataTableFilterDateComponent({ options }: IField) {
  const [dates, setDates] = useState<Date[]>([]);

  // Clear filter value where user presses clear.
  useEffect(() => {
    if (options.value === null) setDates([]);
  }, [options.value]);

  const onHide = () => {
    const result: string[] = [];

    if (dates.every((x) => x !== null) && dates.length === 2) {
      let dateCleaned = new Date(
        dates[0].getFullYear(),
        dates[0].getMonth(),
        dates[0].getDate(),
        0,
        0,
        0,
        0
      );
      result.push(dateCleaned.toISOString());

      dateCleaned = new Date(
        dates[1].getFullYear(),
        dates[1].getMonth(),
        dates[1].getDate(),
        23,
        59,
        59,
        999
      );

      result.push(dateCleaned.toISOString());
      options.filterApplyCallback(result);
    } else {
      setDates([]);
    }
  };

  return (
    <Calendar
      value={dates}
      onChange={(e) => setDates(e.value as [])}
      placeholder="Search"
      selectionMode="range"
      readOnlyInput
      hideOnRangeSelection
      onHide={onHide}
    />
  );
}
