import { useEffect, useMemo } from "react";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";
import { Dropdown } from "primereact/dropdown";
import { InputNumber } from "primereact/inputnumber";
import { Message } from "primereact/message";
import { FormMode } from "../../enum/FormMode";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanRuleStore } from "../../stores/WorkoutPlanRuleStore";
import { WorkoutPlanRuleWeekDto } from "../../model/entities/workout-plan-rule/WorkoutPlanRuleWeekDto";

const MAX_WEEKS = 15;
const MAX_RECORDINGS = 5;

interface IField {
  formMode: FormMode;
  // Passed in by GenericDialogComponent.
  toggleDialogSave?: (isEnabled: boolean) => void;
}

export default function WorkoutPlanRuleFormComponent({
  formMode,
  toggleDialogSave,
}: IField) {
  const { t } = useTranslator();
  const { workoutPlanRuleDto, updateWorkoutPlanRuleDto } =
    useWorkoutPlanRuleStore();

  const isReadOnly = formMode === FormMode.VIEW;

  // The number of days is required. Save stays off while the box is empty.
  const isAwayGapDaysMissing =
    !isReadOnly && workoutPlanRuleDto.awayGapDays == null;

  useEffect(() => {
    if (!isReadOnly) toggleDialogSave?.(!isAwayGapDaysMissing);
  }, [isReadOnly, isAwayGapDaysMissing]);

  const weekCountOptions = useMemo(
    () =>
      Array.from({ length: MAX_WEEKS }, (_, i) => ({
        label: `${i + 1}`,
        value: i + 1,
      })),
    [],
  );

  const maxRecordingOptions = useMemo(
    () =>
      Array.from({ length: MAX_RECORDINGS }, (_, i) => ({
        label: `${i + 1}`,
        value: i + 1,
      })),
    [],
  );

  const weeks = [...(workoutPlanRuleDto.weeks ?? [])].sort(
    (a, b) => a.weekNumber - b.weekNumber,
  );

  // Growing keeps everything already typed; shrinking drops the tail, so it asks first.
  const onWeekCountChange = (nextCount: number) => {
    const current = weeks.length;
    if (nextCount === current) return;

    if (nextCount < current) {
      const confirmed = window.confirm(
        t("Reducing the weeks will delete the text of the removed weeks") + "?",
      );
      if (!confirmed) return;

      updateWorkoutPlanRuleDto({
        weeks: weeks.filter((x) => x.weekNumber <= nextCount),
      });
      return;
    }

    const added: WorkoutPlanRuleWeekDto[] = [];
    for (let weekNumber = current + 1; weekNumber <= nextCount; weekNumber++) {
      const week = new WorkoutPlanRuleWeekDto();
      week.weekNumber = weekNumber;
      week.message = "";
      week.maxRecordings = 1;
      added.push(week);
    }
    updateWorkoutPlanRuleDto({ weeks: [...weeks, ...added] });
  };

  const updateWeek = (
    weekNumber: number,
    updates: Partial<WorkoutPlanRuleWeekDto>,
  ) => {
    updateWorkoutPlanRuleDto({
      weeks: weeks.map((x) =>
        x.weekNumber === weekNumber ? { ...x, ...updates } : x,
      ),
    });
  };

  return (
    <div className="w-full">
      {/* The rule is read only when a workout starts, so editing it while someone
          trains is safe. The note says so, in place of the old lock. */}
      {workoutPlanRuleDto.isLocked && formMode === FormMode.EDIT && (
        <Message
          severity="info"
          className="w-full mb-3 justify-content-start"
          content={
            <div className="flex align-items-start gap-2">
              <i className="pi pi-info-circle mt-1" />
              <div className="flex flex-column gap-1">
                <span className="font-medium">
                  {t("A member is training with this rule right now")}
                </span>
                <span>
                  {t(
                    "Changes apply the next time each member starts a workout, and the workout already running carries on as normal",
                  )}
                </span>
              </div>
            </div>
          }
        />
      )}

      <div className="formgrid grid">
        <div className="field col-12 md:col-8">
          <label
            htmlFor="rule-name"
            className="block text-900 font-medium mb-2"
          >
            {t("Name")}
          </label>
          <InputText
            id="rule-name"
            name="name"
            type="text"
            placeholder={t("Name")}
            value={workoutPlanRuleDto.name}
            className="w-full"
            disabled={isReadOnly}
            onChange={(e) => updateWorkoutPlanRuleDto({ name: e.target.value })}
          />
        </div>

        <div className="field col-12 md:col-4">
          <label
            htmlFor="rule-week-count"
            className="block text-900 font-medium mb-2"
          >
            {t("Number of weeks")}
          </label>
          <Dropdown
            inputId="rule-week-count"
            value={weeks.length || null}
            options={weekCountOptions}
            onChange={(e) => onWeekCountChange(e.value)}
            placeholder={t("Select a value")}
            className="w-full"
            disabled={isReadOnly}
          />
        </div>

        <div className="field col-12 md:col-4">
          <label
            htmlFor="rule-away-gap-days"
            className="block text-900 font-medium mb-2"
          >
            {t("Days away before asking for the week")}
          </label>
          <InputNumber
            inputId="rule-away-gap-days"
            value={workoutPlanRuleDto.awayGapDays ?? null}
            // onChange follows each keystroke, so an emptied box turns red and
            // Save goes off straight away; onValueChange brings in the value
            // clamped to 1-365 when the box loses focus.
            onChange={(e) =>
              updateWorkoutPlanRuleDto({ awayGapDays: e.value ?? null })
            }
            onValueChange={(e) =>
              updateWorkoutPlanRuleDto({ awayGapDays: e.value ?? null })
            }
            min={1}
            max={365}
            showButtons
            className="w-full"
            invalid={isAwayGapDaysMissing}
            disabled={isReadOnly}
          />
          {isAwayGapDaysMissing ? (
            <small className="block mt-1 p-error">
              {t("Enter a number of days")}
            </small>
          ) : (
            <small className="block mt-1 text-color-secondary">
              {t("The default is 12 days")}
            </small>
          )}
        </div>
      </div>

      {weeks.length === 0 && (
        <div className="text-color-secondary">
          {t("Select the number of weeks to continue")}
        </div>
      )}

      {weeks.map((week) => (
        <div
          key={week.weekNumber}
          className="formgrid grid border-top-1 surface-border pt-3"
        >
          <div className="field col-12 md:col-2">
            <span className="block text-primary font-medium mb-2">
              {t("Week")} {week.weekNumber}
            </span>
          </div>

          <div className="field col-12 md:col-7">
            <label
              htmlFor={`rule-week-message-${week.weekNumber}`}
              className="block text-900 font-medium mb-2"
            >
              {t("Message")}
            </label>
            <InputTextarea
              id={`rule-week-message-${week.weekNumber}`}
              rows={2}
              value={week.message ?? ""}
              className="w-full"
              disabled={isReadOnly}
              onChange={(e) =>
                updateWeek(week.weekNumber, { message: e.target.value })
              }
            />
          </div>

          <div className="field col-12 md:col-3">
            <label
              htmlFor={`rule-week-max-${week.weekNumber}`}
              className="block text-900 font-medium mb-2"
            >
              {t("Max recordings")}
            </label>
            <Dropdown
              inputId={`rule-week-max-${week.weekNumber}`}
              value={week.maxRecordings}
              options={maxRecordingOptions}
              onChange={(e) =>
                updateWeek(week.weekNumber, { maxRecordings: e.value })
              }
              className="w-full"
              disabled={isReadOnly}
            />
          </div>
        </div>
      ))}
    </div>
  );
}
