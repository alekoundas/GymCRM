import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useState } from "react";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import { useApiService } from "../../services/ApiService";
import { TrainGroupDto } from "../../model/entities/train-group/TrainGroupDto";
import { useTranslator } from "../../services/TranslatorService";
import TrainGroupParticipantGridComponent from "../train-group-participant/TrainGroupParticipantGridComponent";
import { TrainGroupUnavailableDateDto } from "../../model/entities/train-group-unavailable-date/TrainGroupUnavailableDateDto";

export default function TrainGroupAdminCalendarPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const {
    trainGroupDto,
    resetTrainGroupDto,
    setTrainGroupDto,
    resetSelectedTrainGroupDate,
  } = useTrainGroupStore();
  const [isViewModalVisible, setViewModalVisibility] = useState(false); // Dialog visibility
  const [isAddModalVisible, setAddModalVisibility] = useState(false); // Dialog visibility
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [selectedTrainGroupDateId, setSelectedTrainGroupDateId] =
    useState<number>(0);
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);

  const handleChangeDate = (value: Date) => {
    const dateCleaned = new Date(
      Date.UTC(value.getFullYear(), value.getMonth(), value.getDate(), 0, 0, 0)
    );
    setSelectedDate(dateCleaned);

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = dateCleaned.toISOString();
    timeSlotDto.userId = TokenService.getUserId() ?? "";
    apiService
      .timeslots("TrainGroupDates/TimeSlots", timeSlotDto)
      .then((response) => {
        if (response) {
          setTimeSlots(response);
        }
      });
  };

  const dialogControlView: DialogControl = {
    showDialog: () => setViewModalVisibility(true),
    hideDialog: () => setViewModalVisibility(false),
  };

  const onDateDisable = async (
    trainGroupId: number | undefined
  ): Promise<void> => {
    if (trainGroupId) {
      const trainGroupUnavailableDateDto: TrainGroupUnavailableDateDto = {
        trainGroupId: trainGroupId,
        unavailableDate: selectedDate?.toISOString() ?? "",
        id: 0,
      };

      const response = await apiService.create(
        "trainGroupUnavailableDates",
        trainGroupUnavailableDateDto
      );

      if (response) {
        dialogControlView.hideDialog();
        resetTrainGroupDto();
        resetSelectedTrainGroupDate();
        setTimeSlots([]);
      }
    }
  };

  const onDateEnable = async (
    unavailableTrainGroupId: number | undefined
  ): Promise<void> => {
    if (unavailableTrainGroupId) {
      const response = await apiService.delete(
        "trainGroupUnavailableDates",
        unavailableTrainGroupId
      );

      if (response) {
        dialogControlView.hideDialog();
        resetTrainGroupDto();
        resetSelectedTrainGroupDate();
        setTimeSlots([]);
      }
    }
  };

  return (
    <>
      {/*                  */}
      {/*     Calendar     */}
      {/*                  */}
      <div className="grid w-full">
        <div className="col-12 lg:col-6 xl:col-6">
          <Card
            title={t("Train Groups")}
            subTitle={t("Handle your train groups")}
          >
            <Calendar
              value={selectedDate}
              onChange={(e) => handleChangeDate(e.value as Date)}
              inline
              showIcon={false}
              minDate={new Date()} // Prevent selecting past dates
              className="w-full"
            />
          </Card>
        </div>

        {/*                  */}
        {/*     Timeslots    */}
        {/*                  */}
        <div className=" col-12  lg:col-6 xl:col-6">
          <Card
            header={
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "1rem",
                }}
              >
                <h2 style={{ margin: 0 }}>{t("Available Timeslots")}</h2>
              </div>
            }
          >
            {timeSlots.length === 0 ? (
              <p className="text-gray-500">
                {t("No time slots available for this date")}.
              </p>
            ) : (
              <div>
                <div>
                  {timeSlots
                    .map((x) => x.trainGroupId)
                    .filter(
                      (value, index, array) => array.indexOf(value) === index // Distinct
                    )
                    .map((x) => (
                      <div></div> // display a list of trainers
                    ))}
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {timeSlots
                    ?.sort(
                      (a, b) =>
                        new Date(b.startOn).getTime() -
                        new Date(a.startOn).getTime()
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
                        onClick={() => {
                          apiService
                            .get<TrainGroupDto>(
                              "TrainGroups",
                              slot.trainGroupId
                            )
                            .then((x) => {
                              if (x) {
                                setTrainGroupDto(x);
                                setViewModalVisibility(true);
                                setSelectedTrainGroupDateId(slot.trainGroupId);
                              }
                            });
                        }}
                      />
                    ))}
                </div>
              </div>
            )}
          </Card>
        </div>
      </div>

      {/*                                     */}
      {/*          View Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        formMode={FormMode.VIEW}
        visible={isViewModalVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <div className="flex justify-content-between align-items-center p-3">
            <div className="flex flex-column gap-1"></div>
            <Button
              label={t("Disable for this day")}
              // icon="pi pi-info-circle"
              onClick={() =>
                onDateDisable(
                  timeSlots.find(
                    (x) => x.trainGroupId === selectedTrainGroupDateId
                  )?.trainGroupId
                )
              }
              className="p-button-text"
              visible={
                !timeSlots.find(
                  (x) => x.trainGroupId === selectedTrainGroupDateId
                )?.isUnavailableTrainGroup
              }
            />
            <Button
              label={t("Enable for this day")}
              // icon="pi pi-info-circle"
              onClick={() =>
                onDateEnable(
                  timeSlots.find(
                    (x) => x.trainGroupId === selectedTrainGroupDateId
                  )?.unavailableTrainGroupId
                )
              }
              className="p-button-text"
              visible={
                timeSlots.find(
                  (x) => x.trainGroupId === selectedTrainGroupDateId
                )?.isUnavailableTrainGroup
              }
            />
          </div>
          <TrainGroupFormComponent />
          {/* <TrainGroupDateAdminCalenndarGridComponent /> */}
          <TrainGroupParticipantGridComponent
            trainGroupId={selectedTrainGroupDateId}
            selectedDate={selectedDate ?? new Date()}
          />
        </div>
      </GenericDialogComponent>
    </>
  );
}
