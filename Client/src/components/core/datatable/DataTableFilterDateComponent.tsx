import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { FormEvent } from "primereact/ts-helpers";
import { SyntheticEvent, useState } from "react";

interface IField {
  options: ColumnFilterElementTemplateOptions;
}

export default function DataTableFilterDateComponent({ options }: IField) {
  const [dates, setDates] = useState<Date[]>([]);

  const onChange = (
    e: FormEvent<(Date | null)[], SyntheticEvent<Element, Event>>
  ) => {
    if (e.value?.length) {
      setDates(e.value as []);
    }
  };

  const onHide = () => {
    const result: string[] = [];

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
  };

  return (
    <Calendar
      value={dates}
      onChange={onChange}
      selectionMode="range"
      readOnlyInput
      hideOnRangeSelection
      onHide={onHide}
    />
  );
}
