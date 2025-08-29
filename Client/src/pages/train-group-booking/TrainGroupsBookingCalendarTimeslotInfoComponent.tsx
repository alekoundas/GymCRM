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
  const {
    timeSlotRequestDto,
    timeSlotResponseDto,
    selectedTimeSlotResponseDto,
    setTimeSlotRequestDto,
    setTimeSlotResponseDto,
    setSelectedTimeSlotResponseDto,
    resetTimeSlotResponseDto,
  } = useTrainGroupBookingStore();

  return (
    <>
      {timeSlotRequestDto.selectedDate && selectedTimeSlotResponseDto && (
        <Card
          title="Selected Time Slot Details"
          className="mt-4"
        >
          <p>
            <strong>Time:</strong>{" "}
            {new Date(selectedTimeSlotResponseDto.startOn).toLocaleTimeString()}
          </p>
          <p>
            <strong>Group Name:</strong>{" "}
            {selectedTimeSlotResponseDto.title || "N/A"}
          </p>
          <p>
            <strong>Trainer:</strong>{" "}
            {selectedTimeSlotResponseDto.trainerId || "N/A"}
          </p>
          <p>
            <strong>Description:</strong>{" "}
            {selectedTimeSlotResponseDto.description ||
              "No description available."}
          </p>
          <p>
            <strong>Spots Left:</strong>{" "}
            {selectedTimeSlotResponseDto.spotsLeft !== undefined
              ? selectedTimeSlotResponseDto.spotsLeft
              : "N/A"}
          </p>
          <p>
            <strong>Available:</strong>{" "}
            {selectedTimeSlotResponseDto.spotsLeft > 0 ? "Yes" : "No"}
          </p>

          <h3 className="mt-6">Group is also recurring on </h3>

          <p></p>

          {selectedTimeSlotResponseDto.recurrenceDates.filter(
            (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
          ).length > 0 ? (
            <p>
              <strong>Days of the week:</strong>
            </p>
          ) : null}

          <div>
            {selectedTimeSlotResponseDto.recurrenceDates
              .filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK
              )
              .map((x) => (
                <Tag
                  className="p-2 m-1"
                  key={x.trainGroupDateId}
                  value={DateService.getDayOfWeekFromDate(new Date(x.date))}
                ></Tag>
              ))}
          </div>

          {selectedTimeSlotResponseDto.recurrenceDates.filter(
            (x) => x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
          ).length > 0 ? (
            <p>
              <strong>Days of the month:</strong>
            </p>
          ) : null}

          <div>
            {selectedTimeSlotResponseDto.recurrenceDates
              .filter(
                (x) =>
                  x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH
              )
              .map((x) => (
                <Tag
                  className="p-2 m-1"
                  key={x.trainGroupDateId}
                  value={new Date(x.date).getDate()}
                ></Tag>
              ))}
          </div>
          <div className="flex justify-content-center">
            <Button
              label="Book Now"
              icon="pi pi-check"
              className="mt-4 pr-5 pl-5 pt-3 pb-3"
              onClick={onBook}
              disabled={!(selectedTimeSlotResponseDto.spotsLeft > 0)}
            />
          </div>
        </Card>
      )}
    </>
  );
}
