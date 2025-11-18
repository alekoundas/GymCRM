import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useEffect, useState } from "react";
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
import TrainGroupAttendanceFormComponent from "../train-group-attendance/TrainGroupAttendanceFormComponent";
import { useTrainGroupAttendanceStore } from "../../stores/TrainGroupAttendanceStore";
import { TrainGroupAttendanceDto } from "../../model/entities/train-group-attendance/TrainGroupAttendanceDto";

export default function TrainGroupAdminCalendarPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const {
    trainGroupDto,
    resetTrainGroupDto,
    setTrainGroupDto,
    resetSelectedTrainGroupDate,
  } = useTrainGroupStore();
  const { selectedUserIds, resetSelectedUserIds } =
    useTrainGroupAttendanceStore();

  const [isTakeAttendancesModalVisible, setTakeAttendancesModalVisibility] =
    useState(false); // Dialog visibility
  const [isViewModalVisible, setViewModalVisibility] = useState(false); // Dialog visibility
  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [selectedTrainGroupId, setSelectedTrainGroupId] = useState<number>(0);
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);

  useEffect(() => {
    resetTrainGroupDto();
    resetSelectedTrainGroupDate();
    handleChangeDate(new Date());
  }, []);

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

  const dialogControlTakeAttendances: DialogControl = {
    showDialog: () => setTakeAttendancesModalVisibility(true),
    hideDialog: () => setTakeAttendancesModalVisibility(false),
  };
  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };
  const dialogControlView: DialogControl = {
    showDialog: () => setViewModalVisibility(true),
    hideDialog: () => setViewModalVisibility(false),
  };

  const onSaveAttendances = async (): Promise<void> => {
    const AddData = selectedUserIds.map((id) => {
      const dto = new TrainGroupAttendanceDto();
      dto.attendanceDate = selectedDate?.toISOString() ?? "";
      dto.trainGroupId = selectedTrainGroupId;
      dto.userId = id;
      return dto;
    });
    apiService
      .createRange("TrainGroupAttendances", AddData)
      .then((response) => {
        if (response) {
          dialogControlTakeAttendances.hideDialog();
          resetSelectedUserIds();
        }
      });
  };
  const onDateDisable = async (): Promise<void> => {
    if (selectedTrainGroupId) {
      const trainGroupUnavailableDateDto: TrainGroupUnavailableDateDto = {
        trainGroupId: selectedTrainGroupId,
        unavailableDate: selectedDate?.toISOString() ?? "",
        id: 0,
      };

      const response = await apiService.create(
        "trainGroupUnavailableDates",
        trainGroupUnavailableDateDto
      );

      if (response) {
        dialogControlDelete.hideDialog();
        dialogControlView.hideDialog();
        resetTrainGroupDto();
        resetSelectedTrainGroupDate();
        setTimeSlots([]);
        handleChangeDate(new Date());
      }
    }
  };

  const onDateEnable = async (): Promise<void> => {
    const unavailableTrainGroupId = timeSlots.find(
      (x) => x.trainGroupId === selectedTrainGroupId
    )?.unavailableTrainGroupId;

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
        handleChangeDate(new Date());
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
                                setSelectedTrainGroupId(slot.trainGroupId);
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
            <Button
              label={t("Take attendances")}
              onClick={() => dialogControlTakeAttendances.showDialog()}
              severity="info"
            />
            <div></div>
            <Button
              label={t("Disable for this day")}
              onClick={() => dialogControlDelete.showDialog()}
              severity="danger"
              visible={
                !timeSlots.find((x) => x.trainGroupId === selectedTrainGroupId)
                  ?.isUnavailableTrainGroup
              }
            />
            <Button
              label={t("Enable for this day")}
              onClick={() => onDateEnable()}
              severity="success"
              visible={
                timeSlots.find((x) => x.trainGroupId === selectedTrainGroupId)
                  ?.isUnavailableTrainGroup
              }
            />
          </div>

          <TrainGroupFormComponent />

          <TrainGroupParticipantGridComponent
            trainGroupId={selectedTrainGroupId}
            selectedDate={selectedDate ?? new Date()}
          />
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Delete Warning               */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDateDisable}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>

      {/*                                       */}
      {/*          Take attendances             */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isTakeAttendancesModalVisible}
        control={dialogControlTakeAttendances}
        onSave={onSaveAttendances}
        formMode={FormMode.ADD}
      >
        <div className="flex justify-content-center">
          <TrainGroupAttendanceFormComponent />
        </div>
      </GenericDialogComponent>
    </>
  );
}
