import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTrainGroupBookingStore } from "../../stores/TrainGroupBookingStore";
import { useTranslator } from "../../services/TranslatorService";

export default function TrainGroupsBookingCalendarTimeslotComponent() {
  const { t } = useTranslator();
  const { timeSlotResponseDto, selectedTimeSlot, setSelectedTimeSlot } =
    useTrainGroupBookingStore();

  return (
    <>
      {/*                  */}
      {/*     Timeslots    */}
      {/*                  */}
      <Card title={t("Available Time Slots")}>
        {timeSlotResponseDto?.length === 0 ? (
          <>
            <p className="text-gray-500">
              {t("No time slots available for this date.")}
            </p>
          </>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {timeSlotResponseDto
              ?.sort(
                (a, b) =>
                  new Date(b.startOn).getTime() - new Date(a.startOn).getTime()
              )
              ?.map((slot) => (
                <Button
                  key={slot.trainGroupDateId}
                  label={
                    new Date(slot.startOn)
                      .getUTCHours()
                      .toString()
                      .padStart(2, "0") +
                    ":" +
                    new Date(slot.startOn)
                      .getUTCMinutes()
                      .toString()
                      .padStart(2, "0") +
                    " - " +
                    slot.title
                  }
                  className={
                    selectedTimeSlot?.trainGroupDateId === slot.trainGroupDateId
                      ? "p-button-raised p-button-primary"
                      : "p-button-outlined"
                  }
                  onClick={() => setSelectedTimeSlot(slot)}
                />
              ))}
          </div>
        )}
      </Card>
    </>
  );
}
