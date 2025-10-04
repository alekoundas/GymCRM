import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Tag } from "primereact/tag";
import { DividerComponent } from "../../components/core/divider/DividerComponent";
import { JSX } from "react";

interface IField {
  onBook: () => void;
}

export default function TrainGroupsBookingCalendarTimeslotInfoComponent({
  onBook,
}: IField) {
  const { timeSlotRequestDto, selectedTimeSlot, timeSlotResponseDto } =
    useTrainGroupBookingStore();

  // Helper to generate label for a date based on type
  const getDateLabel = (
    date: string,
    type: TrainGroupDateTypeEnum | undefined
  ): string => {
    switch (type) {
      case TrainGroupDateTypeEnum.DAY_OF_WEEK:
        return DateService.getDayOfWeekFromDate(new Date(date)) ?? "";
      case TrainGroupDateTypeEnum.DAY_OF_MONTH:
        return new Date(date).getDate().toString();
      case TrainGroupDateTypeEnum.FIXED_DAY:
      case undefined: // One-off
        return new Date(date).toLocaleDateString("en-GB");
      default:
        return "";
    }
  };

  // Helper to render a Tag for a date
  const renderDateTag = (
    type: TrainGroupDateTypeEnum | undefined
  ): JSX.Element[] => {
    const htmlElements: JSX.Element[] = [];

    selectedTimeSlot!.recurrenceDates
      .filter((x) => x.trainGroupDateType === type)
      .forEach((date) => {
        const label = getDateLabel(date.date, type);
        let isJoined = date.isUserJoined;

        if (type === undefined)
          isJoined = selectedTimeSlot!.recurrenceDates.some(
            (x) =>
              x.trainGroupDateId === date.trainGroupDateId &&
              x.trainGroupDateType !== undefined &&
              x.isUserJoined
          );
        htmlElements.push(
          <Tag
            key={date.trainGroupDateId}
            className="p-2 m-1"
            severity={isJoined ? "success" : undefined}
          >
            {label}
            {isJoined && <i className="pi pi-check ml-1" />}
          </Tag>
        );
      });

    return htmlElements;
  };

  return (
    <>
      {timeSlotRequestDto.selectedDate && selectedTimeSlot && (
        <Card
          title="Selected Time Slot Details"
          className="mt-4"
        >
          <p>
            <strong>Time:</strong>{" "}
            {new Date(selectedTimeSlot.startOn).toLocaleTimeString()}
          </p>
          <p>
            <strong>Group Name:</strong> {selectedTimeSlot.title || "N/A"}
          </p>
          <p>
            <strong>Trainer:</strong> {selectedTimeSlot.trainerId || "N/A"}
          </p>
          <p>
            <strong>Spots Left:</strong>{" "}
            {selectedTimeSlot.spotsLeft !== undefined
              ? selectedTimeSlot.spotsLeft
              : "N/A"}
          </p>
          <p>
            <strong>Available:</strong>{" "}
            {selectedTimeSlot.spotsLeft > 0 ? "Yes" : "No"}
          </p>
          <p>
            <strong>Description:</strong>{" "}
            {selectedTimeSlot.description || "No description available."}
          </p>

          <h3 className="mt-6">Group is occurring on </h3>

          {/*                      */}
          {/*     Current Date     */}
          {/*                      */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === undefined
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">One-off</strong>
                  </p>
                </DividerComponent>

                {renderDateTag(undefined)}

                {/* {selectedTimeSlot.recurrenceDates
                  .filter((x) => x.trainGroupDateType === undefined)
                  .map((x) => {
                    if (x.isUserJoined)
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                          severity={"success"}
                        >
                          {new Date(x.date).getDate() +
                            "/" +
                            (new Date(x.date).getMonth() + 1) +
                            "/" +
                            new Date(x.date).getFullYear()}
                          {"  "}
                          <i className="pi pi-check"></i>
                        </Tag>
                      );
                    else
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                        >
                          {new Date(x.date).getDate() +
                            "/" +
                            (new Date(x.date).getMonth() + 1) +
                            "/" +
                            new Date(x.date).getFullYear()}
                        </Tag>
                      );
                  })} */}
              </>
            )}
          </div>

          {/*                      */}
          {/*       FIXED_DAY      */}
          {/*                      */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
            ) && (
              <>
                {/* <p>
                  <strong>One-off:</strong>
                </p> */}
                <DividerComponent>
                  <p>
                    <strong className="text-base">One-off</strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.FIXED_DAY)}
                {/* {selectedTimeSlot.recurrenceDates
                  .filter(
                    (x) =>
                      x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
                  )
                  .map((x) => {
                    if (x.isUserJoined)
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                          severity={"success"}
                        >
                          {new Date(x.date).getDate() +
                            "/" +
                            (new Date(x.date).getMonth() + 1) +
                            "/" +
                            new Date(x.date).getFullYear()}
                          {"  "}
                          <i className="pi pi-check"></i>
                        </Tag>
                      );
                    else
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                        >
                          {new Date(x.date).getDate() +
                            "/" +
                            (new Date(x.date).getMonth() + 1) +
                            "/" +
                            new Date(x.date).getFullYear()}
                        </Tag>
                      );
                  })} */}
              </>
            )}
          </div>

          {/*                        */}
          {/*       DAY_OF_WEEK      */}
          {/*                        */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">
                      Recurring Dates (Day of week)
                    </strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.DAY_OF_WEEK)}

                {/* {selectedTimeSlot.recurrenceDates
                  .filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_WEEK
                  )
                  .map((x) => {
                    if (x.isUserJoined)
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                          severity={"success"}
                        >
                          {DateService.getDayOfWeekFromDate(new Date(x.date))}{" "}
                          <i className="pi pi-check"></i>
                        </Tag>
                      );
                    else
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                        >
                          {DateService.getDayOfWeekFromDate(new Date(x.date))}
                        </Tag>
                      );
                  })} */}
              </>
            )}
          </div>

          {/*                        */}
          {/*       DAY_OF_MONTH     */}
          {/*                        */}
          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) =>
                x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
            ) && (
              <>
                <DividerComponent>
                  <p>
                    <strong className="text-base">
                      Recurring Dates (Day of month)
                    </strong>
                  </p>
                </DividerComponent>
                {renderDateTag(TrainGroupDateTypeEnum.DAY_OF_MONTH)}

                {/* {selectedTimeSlot.recurrenceDates
                  .filter(
                    (x) =>
                      x.trainGroupDateType ===
                      TrainGroupDateTypeEnum.DAY_OF_MONTH
                  )
                  .map((x) => {
                    if (x.isUserJoined)
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                          severity={"success"}
                        >
                          {new Date(x.date).getDate()}{" "}
                          <i className="pi pi-check"></i>
                        </Tag>
                      );
                    else
                      return (
                        <Tag
                          className="p-2 m-1"
                          key={x.trainGroupDateId}
                        >
                          {new Date(x.date).getDate()}
                        </Tag>
                      );
                  })} */}
              </>
            )}
          </div>

          <div className="flex justify-content-center">
            <Button
              label="Book Now"
              icon="pi pi-check"
              className="mt-4 pr-5 pl-5 pt-3 pb-3"
              onClick={onBook}
              // disabled={!(selectedTimeSlot.spotsLeft > 0)}
            />
          </div>
        </Card>
      )}
    </>
  );
}
