import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { useState } from "react";
import { TokenService } from "../../services/TokenService";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import TrainGroupDateAdminCalenndarGridComponent from "../train-group-date/TrainGroupDateAdminCalenndarGridComponent";

export default function TrainGroupAdminCalendarPage() {
  const { trainGroupDto, resetTrainGroupDto } = useTrainGroupStore();
  const [isViewModalVisible, setViewModalVisibility] = useState(false); // Dialog visibility
  const [isAddModalVisible, setAddModalVisibility] = useState(false); // Dialog visibility
  const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);

  const handleChangeDate = (value: Date) => {
    setSelectedDate(value);

    const timeSlotDto = new TimeSlotRequestDto();
    timeSlotDto.selectedDate = value.toISOString();
    timeSlotDto.userId = TokenService.getUserId() ?? "";
    ApiService.timeslots("TrainGroupDates/TimeSlots", timeSlotDto).then(
      (response) => {
        if (response) {
          setTimeSlots(response);
        }
      }
    );
  };

  const dialogControlAdd: DialogControl = {
    showDialog: () => setAddModalVisibility(true),
    hideDialog: () => setAddModalVisibility(false),
  };

  const dialogControlView: DialogControl = {
    showDialog: () => setViewModalVisibility(true),
    hideDialog: () => setViewModalVisibility(false),
  };

  const onSaveAdd = async (): Promise<void> => {
    const response = await ApiService.create("trainGroups", trainGroupDto);

    if (response) {
      dialogControlAdd.hideDialog();
      resetTrainGroupDto();
    }
  };

  const onSaveEdit = async (): Promise<void> => {
    const response = await ApiService.update(
      "trainGroups",
      trainGroupDto,
      trainGroupDto.id
    );

    if (response) {
      dialogControlView.hideDialog();
      resetTrainGroupDto();
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
            title="Train Groups"
            subTitle="Handle your train groups"
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
                <h2 style={{ margin: 0 }}>Available Timeslots</h2>
                <Button
                  className="m-2"
                  type="button"
                  icon="pi pi-plus"
                  label="Add"
                  outlined
                  visible={TokenService.isUserAllowed("TrainGroups_Add")}
                  onClick={() => {
                    setAddModalVisibility(true);
                    resetTrainGroupDto();
                  }}
                />
              </div>
            }
          >
            {timeSlots.length === 0 ? (
              <p className="text-gray-500">
                No time slots available for this date.
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
                  {timeSlots.map((slot) => (
                    <Button
                      key={slot.trainGroupDateId}
                      label={
                        (new Date(slot.startOn).getHours().toString().length ===
                        1
                          ? "0" + new Date(slot.startOn).getHours().toString()
                          : new Date(slot.startOn).getHours()) +
                        ":" +
                        (new Date(slot.startOn).getMinutes().toString()
                          .length === 1
                          ? "0" + new Date(slot.startOn).getMinutes().toString()
                          : new Date(slot.startOn).getMinutes())
                      }
                      onClick={() => {
                        resetTrainGroupDto(slot.trainGroupId).then((x) => {
                          setViewModalVisibility(true);
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
      {/*           Add Train Group           */}
      {/*                                     */}

      <GenericDialogComponent
        visible={isAddModalVisible}
        control={dialogControlAdd}
        onSave={onSaveAdd}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.ADD} />
          <TrainGroupDateAdminCalenndarGridComponent formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent>

      {/*                                     */}
      {/*          View Train Group           */}
      {/*                                     */}
      <GenericDialogComponent
        visible={isViewModalVisible}
        control={dialogControlView}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.VIEW} />
          <TrainGroupDateAdminCalenndarGridComponent formMode={FormMode.VIEW} />
        </div>
      </GenericDialogComponent>
    </>
  );
}
