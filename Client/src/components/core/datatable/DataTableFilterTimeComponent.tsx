import { ColumnFilterElementTemplateOptions } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { useEffect, useRef, useState } from "react";
import { InputText } from "primereact/inputtext";
import { OverlayPanel } from "primereact/overlaypanel";

interface IField {
  options: ColumnFilterElementTemplateOptions;
}

export default function DataTableFilterTimeComponent({ options }: IField) {
  const emptyDate: Date = new Date(2000, 0, 1, 0, 0, 0, 0);

  const [startTime, setStartTime] = useState<Date>(emptyDate);
  const [endTime, setEndTime] = useState<Date>(emptyDate);
  const overlayPanel = useRef<OverlayPanel>(null);

  // Clear filter value where user presses clear.
  useEffect(() => {
    if (options.value === null) {
      setStartTime(emptyDate);
      setEndTime(emptyDate);
    }
  }, [options.value]);

  const onHide = () => {
    const result: string[] = [];

    if (
      (startTime.getHours() === emptyDate.getHours() &&
        startTime.getMinutes() === emptyDate.getMinutes() &&
        endTime.getHours() === emptyDate.getHours() &&
        endTime.getMinutes() === emptyDate.getMinutes()) ||
      startTime > endTime
    ) {
      setStartTime(emptyDate);
      setEndTime(emptyDate);
    } else {
      const dateStartCleaned = new Date(
        2000,
        0,
        1,
        startTime.getHours(),
        startTime.getMinutes(),
        0,
        0
      );
      const dateEndCleaned = new Date(
        2000,
        0,
        1,
        endTime.getHours(),
        endTime.getMinutes(),
        0,
        0
      );

      result.push(dateStartCleaned.toISOString());
      result.push(dateEndCleaned.toISOString());
      options.filterApplyCallback(result);
    }
  };

  const displayValue = () => {
    if (
      (startTime.getHours() === emptyDate.getHours() &&
        startTime.getMinutes() === emptyDate.getMinutes() &&
        endTime.getHours() === emptyDate.getHours() &&
        endTime.getMinutes() === emptyDate.getMinutes()) ||
      startTime > endTime
    )
      return "";

    return startTime && endTime
      ? `${startTime.toLocaleTimeString([], {
          hour: "2-digit",
          minute: "2-digit",
          hour12: false,
        })} - ${endTime.toLocaleTimeString([], {
          hour: "2-digit",
          minute: "2-digit",
          hour12: false,
        })}`
      : "";
  };

  return (
    <>
      <InputText
        value={displayValue()}
        readOnly
        // placeholder="Select Time Range"
        placeholder="Search"
        onClick={(e) => overlayPanel.current?.show(e, e.target)}
      />
      <OverlayPanel
        ref={overlayPanel}
        onHide={onHide}
        dismissable
      >
        <div className="flex">
          <div className="p-field">
            <p>Start Time</p>
            <Calendar
              value={startTime}
              onChange={(e) => setStartTime(e.value as Date)}
              showTime
              timeOnly
              hourFormat="24"
              inline
            />
          </div>
          <div className="p-field">
            <p>End Time</p>
            <Calendar
              value={endTime}
              onChange={(e) => setEndTime(e.value as Date)}
              showTime
              timeOnly
              hourFormat="24"
              inline
            />
          </div>
        </div>
      </OverlayPanel>
    </>
  );
}
