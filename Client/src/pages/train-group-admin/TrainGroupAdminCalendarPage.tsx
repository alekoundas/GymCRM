import { Button } from "primereact/button";
import { Calendar } from "primereact/calendar";
import { Card } from "primereact/card";
import { Badge } from "primereact/badge";
import { TabPanel, TabView } from "primereact/tabview";
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
import { LookupDto } from "../../model/lookup/LookupDto";
import { formatClock } from "../train-group-booking/BookingDates";
import { CategoryDotComponent } from "../../components/core/category/CategoryTagComponent";
import {
  NO_CATEGORY_COLOR,
  categoryColorOf,
  categoryTintStyle,
} from "../../services/CategoryColorService";

interface CategoryTab {
  // Undefined for "All", null for the groups in no category.
  id?: number | null;
  name: string;
  color?: string;
}

// The day the calendar is on, with no time of day. Local parts on purpose: the
// admin means the date they can see, not whatever day it is in UTC at the time.
const toSelectedDate = (value: Date): Date =>
  new Date(Date.UTC(value.getFullYear(), value.getMonth(), value.getDate()));

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
  const [selectedDate, setSelectedDate] = useState<Date | null>(
    toSelectedDate(new Date()),
  );
  const [selectedTrainGroupId, setSelectedTrainGroupId] = useState<number>(0);
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);

  // One tab per category the admin has set up, after "All".
  const [categories, setCategories] = useState<CategoryTab[]>([]);
  const [activeTab, setActiveTab] = useState(0);

  useEffect(() => {
    resetTrainGroupDto();
    resetSelectedTrainGroupDate();
    handleChangeDate(new Date());

    const lookupDto = new LookupDto();
    lookupDto.take = 1000;
    apiService.getDataLookup("TrainGroupCategories", lookupDto).then((response) => {
      if (response?.data)
        setCategories(
          response.data
            .filter((x) => x.id)
            .map((x) => ({
              id: +x.id!,
              name: x.value ?? "",
              color: categoryColorOf({ id: +x.id!, color: x.color }),
            }))
        );
    });
  }, []);

  const handleChangeDate = (value: Date) => {
    const dateCleaned = toSelectedDate(value);
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

  // Bumped whenever the attendances dialog closes, so the grid underneath picks up
  // the rows that were just taken. On close rather than on save: a save that only
  // partly went through still leaves the grid showing what is really there.
  const [attendancesVersion, setAttendancesVersion] = useState(0);

  const dialogControlTakeAttendances: DialogControl = {
    showDialog: () => setTakeAttendancesModalVisibility(true),
    hideDialog: () => {
      setTakeAttendancesModalVisibility(false);
      setAttendancesVersion((previous) => previous + 1);
    },
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

  // A group's category, if it still has one that exists.
  const categoryOf = (slot: TimeSlotResponseDto): CategoryTab | undefined =>
    categories.find((x) => x.id === slot.trainGroupCategoryId);

  const noCategoryIndex = categories.length + 1;
  const hasUncategorised = timeSlots.some((x) => !categoryOf(x));
  const tabs: CategoryTab[] = [
    { id: undefined, name: t("All") },
    ...categories,
    ...(categories.length > 0 && (hasUncategorised || activeTab === noCategoryIndex)
      ? [{ id: null, name: t("No category"), color: NO_CATEGORY_COLOR }]
      : []),
  ];

  // Each group in its category's colour. A gym with no categories keeps the plain
  // buttons - there would be nothing to tell apart.
  const slotStyle = (slot: TimeSlotResponseDto) => {
    if (categories.length === 0) return undefined;

    const color = categoryOf(slot)?.color ?? NO_CATEGORY_COLOR;
    return { ...categoryTintStyle(color), borderLeft: `4px solid ${color}` };
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
        <div className="col-12 lg:col-6 xl:col-6">
          <Card title={t("Available Timeslots")}>
            {/* The day's groups, all together, then one tab per category in its
                colour, then the groups in none - grey, and only on a day that has
                some (or while that tab is open). */}
            <TabView
              scrollable
              activeIndex={activeTab < tabs.length ? activeTab : 0}
              onTabChange={(e) => setActiveTab(e.index)}
            >
              {tabs.map(
                (tab) => {
                  const tabSlots = [...timeSlots]
                    .filter(
                      (x) =>
                        tab.id === undefined ||
                        (tab.id === null
                          ? !categoryOf(x)
                          : x.trainGroupCategoryId === tab.id)
                    )
                    .sort((a, b) =>
                      formatClock(a.startOn).localeCompare(formatClock(b.startOn))
                    );

                  return (
                    <TabPanel
                      key={tab.id === undefined ? "all" : (tab.id ?? "none")}
                      header={
                        <span className="flex align-items-center gap-2">
                          {tab.color && <CategoryDotComponent color={tab.color} />}
                          {tab.name}
                          <Badge
                            value={tabSlots.length}
                            severity={tabSlots.length > 0 ? undefined : "secondary"}
                          />
                        </span>
                      }
                    >
                      {tabSlots.length === 0 ? (
                        <p className="m-0 text-color-secondary">
                          {t("No time slots available for this date")}.
                        </p>
                      ) : (
                        <div className="flex flex-column gap-2">
                          {tabSlots.map((slot) => (
                            <Button
                              key={slot.trainGroupId}
                              className="w-full text-left"
                              label={`${formatClock(slot.startOn)} - ${slot.title}`}
                              style={slotStyle(slot)}
                              onClick={() => {
                                apiService
                                  .get<TrainGroupDto>("TrainGroups", slot.trainGroupId)
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
                      )}
                    </TabPanel>
                  );
                }
              )}
            </TabView>
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
            refreshToken={attendancesVersion}
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
