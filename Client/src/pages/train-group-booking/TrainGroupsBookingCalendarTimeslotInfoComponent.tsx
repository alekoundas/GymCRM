import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { DateService } from "../../services/DateService";
import { Tag } from "primereact/tag";

interface IField {
  onBook: () => void;
}

export default function TrainGroupsBookingCalendarTimeslotInfoComponent({
  onBook,
}: IField) {
  const { timeSlotRequestDto, selectedTimeSlot } = useTrainGroupBookingStore();

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
            <strong>Description:</strong>{" "}
            {selectedTimeSlot.description || "No description available."}
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

          <h3 className="mt-6">Group is occurring on </h3>

          <p>
            <strong>Current Date:</strong>{" "}
          </p>

          <div>
            {selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
            ) &&
              selectedTimeSlot.recurrenceDates
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
                })}
          </div>

          <div>
            {!selectedTimeSlot.recurrenceDates.some(
              (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY
            ) &&
              selectedTimeSlot.recurrenceDates
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
                })}
          </div>

          {selectedTimeSlot.recurrenceDates.filter(
            (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
          ).length > 0 ? (
            <p>
              <strong>Days of the week:</strong>
            </p>
          ) : null}

          <div>
            {selectedTimeSlot.recurrenceDates
              .filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
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
              })}
          </div>

          {selectedTimeSlot.recurrenceDates.filter(
            (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
          ).length > 0 ? (
            <p>
              <strong>Days of the month:</strong>
            </p>
          ) : null}

          <div>
            {selectedTimeSlot.recurrenceDates
              .filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
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
              })}
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
