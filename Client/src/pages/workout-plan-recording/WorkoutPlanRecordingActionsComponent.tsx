import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { Message } from "primereact/message";
import { Tag } from "primereact/tag";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";
import { WorkoutPlanStartScenario } from "../../enum/WorkoutPlanStartScenario";
import { WorkoutPlanStartContextDto } from "../../model/entities/workout-plan-recording/WorkoutPlanStartContextDto";
import { formatDuration } from "./WorkoutPlanRecordingGridComponent";

interface IField {
  workoutPlanId: number;
  // The context is computed from the rule and the week, so both are watched here.
  // Without them an administrator setting a rule leaves the bar on "no rule yet"
  // until the page is reloaded.
  workoutPlanRuleId?: number;
  currentWeek?: number;
  // The admin sees the timer but never drives it - a recording belongs to the member.
  isAdminPage: boolean;
  onChanged?: () => void;
}

export default function WorkoutPlanRecordingActionsComponent({
  workoutPlanId,
  workoutPlanRuleId,
  currentWeek,
  isAdminPage,
  onChanged,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const [startContext, setStartContext] =
    useState<WorkoutPlanStartContextDto | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState<number>(0);
  const [isDialogVisible, setDialogVisibility] = useState(false);
  const [selectedWeek, setSelectedWeek] = useState<number>(1);
  const [isBusy, setBusy] = useState(false);

  const intervalRef = useRef<number | undefined>(undefined);

  const isPlanOwner =
    !isAdminPage && TokenService.isUserAllowed("WorkoutPlanRecordings_Add");

  const loadStartContext = useCallback(async () => {
    const response = await apiService.getWorkoutPlanStartContext(workoutPlanId);
    setStartContext(response);
    // The server sends elapsed seconds, so a page reload, a new login or another
    // device all pick the timer up where it actually is.
    setElapsedSeconds(response?.runningRecording?.elapsedSeconds ?? 0);
    setSelectedWeek(response?.currentWeek ?? response?.nextWeek ?? 1);
  }, [apiService, workoutPlanId]);

  useEffect(() => {
    if (workoutPlanId) loadStartContext();
  }, [workoutPlanId, workoutPlanRuleId, currentWeek]);

  // Ticks locally between refreshes; the server value stays the source of truth.
  useEffect(() => {
    if (startContext?.scenario !== WorkoutPlanStartScenario.Running) {
      if (intervalRef.current) window.clearInterval(intervalRef.current);
      return;
    }

    intervalRef.current = window.setInterval(
      () => setElapsedSeconds((previous) => previous + 1),
      1000,
    );

    return () => {
      if (intervalRef.current) window.clearInterval(intervalRef.current);
    };
  }, [startContext?.scenario]);

  const onStartClick = () => {
    if (!startContext) return;
    setSelectedWeek(startContext.currentWeek ?? startContext.nextWeek);
    setDialogVisibility(true);
  };

  const onConfirmStart = async (weekNumber: number) => {
    setBusy(true);
    const response = await apiService.startWorkoutPlanRecording(
      workoutPlanId,
      weekNumber,
    );
    setBusy(false);

    if (!response) return;

    setDialogVisibility(false);
    await loadStartContext();
    if (onChanged) onChanged();
  };

  const onStopClick = async () => {
    const recordingId = startContext?.runningRecording?.id;
    if (!recordingId) return;

    setBusy(true);
    const response = await apiService.stopWorkoutPlanRecording(recordingId);
    setBusy(false);

    if (!response) return;

    await loadStartContext();
    if (onChanged) onChanged();
  };

  if (!startContext) return <></>;

  const scenario = startContext.scenario;
  const isRunning = scenario === WorkoutPlanStartScenario.Running;

  //
  //          The action bar. It sticks to the bottom of the scroll so the
  //          timer stays reachable however far down the exercises you are.
  //
  const renderActions = () => {
    if (scenario === WorkoutPlanStartScenario.NoRule)
      return (
        <div className="flex align-items-center gap-2 text-sm text-color-secondary">
          <i className="pi pi-info-circle" />
          <span>{t("The trainer has not set a rule for this plan yet")}</span>
        </div>
      );

    if (isRunning)
      return (
        <div className="flex align-items-center justify-content-between gap-3">
          <div className="flex-1 min-w-0">
            <div className="flex align-items-center gap-2">
              <span
                className="border-circle bg-green-400 flex-none"
                style={{ width: "0.5rem", height: "0.5rem" }}
              />
              <span className="text-xs uppercase text-color-secondary">
                {t("In progress")}
              </span>
            </div>
            <div className="text-3xl font-semibold line-height-2">
              {formatDuration(elapsedSeconds)}
            </div>
          </div>

          {isPlanOwner ? (
            <Button
              label={t("Finish")}
              icon="pi pi-stop-circle"
              severity="danger"
              outlined
              size="large"
              disabled={isBusy}
              onClick={onStopClick}
            />
          ) : (
            <span className="text-xs text-color-secondary text-right">
              {t("Only the member can start and stop the timer")}
            </span>
          )}
        </div>
      );

    return (
      <Button
        label={t("Start workout")}
        icon="pi pi-play"
        className="w-full md:w-auto"
        size="large"
        disabled={!isPlanOwner || isBusy}
        onClick={onStartClick}
      />
    );
  };

  //
  //          The Start dialog, one branch per scenario
  //
  const renderDialogBody = () => {
    switch (scenario) {
      case WorkoutPlanStartScenario.FirstEver:
        return (
          <p className="m-0">
            {t("You have not started this plan yet")}.{" "}
            {t("It will start on week")} 1.
          </p>
        );

      case WorkoutPlanStartScenario.UnderMax:
        return (
          <p className="m-0">
            {t("Recordings on this week")}:{" "}
            <strong>
              {startContext.recordingsOnCurrentWeek} / {startContext.maxRecordings}
            </strong>
          </p>
        );

      case WorkoutPlanStartScenario.AtMax:
        return (
          <p className="m-0">
            {t("You have used all recordings for this week")} (
            {startContext.maxRecordings}).
          </p>
        );

      case WorkoutPlanStartScenario.AwayTooLong:
        return (
          <div>
            <p className="mt-0">
              {t("Days since last recording")}:{" "}
              <strong>{startContext.daysSinceLastRecording}</strong>.{" "}
              {t("Choose where to continue from")}.
            </p>
            <div className="flex flex-wrap gap-2">
              {startContext.availableWeeks.map((week) => (
                <Button
                  key={week}
                  label={`${t("Week")} ${week}`}
                  outlined={week !== selectedWeek}
                  onClick={() => setSelectedWeek(week)}
                />
              ))}
            </div>
          </div>
        );

      case WorkoutPlanStartScenario.Orphaned:
        return (
          <p className="m-0">
            {t("The number of weeks changed so you start again from week 1")}.{" "}
            {t("Your old recordings stay as they are")}.
          </p>
        );

      default:
        return <></>;
    }
  };

  const renderDialogHeader = () => {
    switch (scenario) {
      case WorkoutPlanStartScenario.UnderMax:
        return t("Same week or next");
      case WorkoutPlanStartScenario.AtMax:
        return t("The week is complete");
      case WorkoutPlanStartScenario.AwayTooLong:
        return t("You have been away for too long");
      case WorkoutPlanStartScenario.Orphaned:
        return t("The rule changed");
      default:
        return t("Start workout");
    }
  };

  const renderDialogFooter = () => {
    const cancel = (
      <Button
        label={t("Cancel")}
        text
        disabled={isBusy}
        onClick={() => setDialogVisibility(false)}
      />
    );

    switch (scenario) {
      case WorkoutPlanStartScenario.UnderMax:
        return (
          <div className="flex flex-column gap-2">
            <Button
              label={`${t("Stay on week")} ${startContext.currentWeek}`}
              icon="pi pi-play"
              disabled={isBusy}
              onClick={() => onConfirmStart(startContext.currentWeek ?? 1)}
            />
            <Button
              label={`${t("Advance to week")} ${startContext.nextWeek}`}
              outlined
              disabled={isBusy}
              onClick={() => onConfirmStart(startContext.nextWeek)}
            />
            {cancel}
          </div>
        );

      case WorkoutPlanStartScenario.AwayTooLong:
        return (
          <div className="flex flex-column gap-2">
            <Button
              label={t("Save and start")}
              icon="pi pi-play"
              disabled={isBusy}
              onClick={() => onConfirmStart(selectedWeek)}
            />
            {cancel}
          </div>
        );

      default:
        return (
          <div className="flex flex-column gap-2">
            <Button
              label={`${t("Start on week")} ${startContext.nextWeek}`}
              icon="pi pi-play"
              disabled={isBusy}
              onClick={() => onConfirmStart(startContext.nextWeek)}
            />
            {cancel}
          </div>
        );
    }
  };

  return (
    <>
      <div className="sticky bottom-0 z-1 surface-card border-top-1 surface-border pt-3 mt-3">
        {renderActions()}
      </div>

      <Dialog
        header={renderDialogHeader()}
        visible={isDialogVisible}
        style={{ width: "min(92vw, 30rem)" }}
        footer={renderDialogFooter()}
        onHide={() => setDialogVisibility(false)}
      >
        {renderDialogBody()}
      </Dialog>
    </>
  );
}
