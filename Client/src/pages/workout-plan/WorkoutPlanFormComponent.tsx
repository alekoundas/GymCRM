import { useEffect, useMemo, useState } from "react";
import { InputText } from "primereact/inputtext";
import { InputTextarea } from "primereact/inputtextarea";
import { Dropdown } from "primereact/dropdown";
import { Checkbox } from "primereact/checkbox";
import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useTranslator } from "../../services/TranslatorService";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanRuleDto } from "../../model/entities/workout-plan-rule/WorkoutPlanRuleDto";
import { useApiService } from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import LookupComponent from "../../components/core/dropdown/LookupComponent";

interface IField extends DialogChildProps {
  isAdminPage: boolean;
}

//
//          The whole plan in one form, saved in one go. It lives in a dialog
//          opened from the header, so the page itself stays a reading view.
//
export default function WorkoutPlanFormComponent({
  formMode,
  isAdminPage,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { workoutPlanDto, updateWorkoutPlanDto } = useWorkoutPlanStore();

  // The plan's weekCount comes from the server and only refreshes on save, so
  // picking a rule here has to read the rule's own week count to offer weeks.
  const [ruleWeekCount, setRuleWeekCount] = useState<number>(
    workoutPlanDto.weekCount ?? 0,
  );

  useEffect(() => {
    setRuleWeekCount(workoutPlanDto.weekCount ?? 0);
  }, [workoutPlanDto.weekCount]);

  const isReadOnly = formMode === FormMode.VIEW;

  // Same gate as the pencil that opens this dialog, so a trainer can set the
  // rule and week while looking at the member's own view of the plan.
  const canManageRule = TokenService.isUserAllowed("WorkoutPlansAdmin_Edit");

  const weekOptions = useMemo(
    () =>
      Array.from({ length: ruleWeekCount }, (_, i) => ({
        label: `${t("Week")} ${i + 1}`,
        value: i + 1,
      })),
    [ruleWeekCount, t],
  );

  const isWeekMissing =
    workoutPlanDto.workoutPlanRuleId !== undefined &&
    workoutPlanDto.workoutPlanRuleId !== null &&
    !workoutPlanDto.currentWeek;

  const onRuleChange = async (ruleId: number | undefined) => {
    updateWorkoutPlanDto({ workoutPlanRuleId: ruleId });

    if (!ruleId) {
      setRuleWeekCount(0);
      updateWorkoutPlanDto({ currentWeek: undefined });
      return;
    }

    const rule = await apiService.get<WorkoutPlanRuleDto>(
      "WorkoutPlanRules",
      ruleId,
    );
    const weekCount = rule?.weekCount ?? 0;
    setRuleWeekCount(weekCount);

    // A week the newly chosen rule does not have must not be saved. Clearing it
    // makes the field invalid, which is the prompt to pick a real one.
    if ((workoutPlanDto.currentWeek ?? 0) > weekCount)
      updateWorkoutPlanDto({ currentWeek: undefined });
  };

  return (
    <div className="w-full">
      <div className="field w-full">
        <label
          htmlFor="title"
          className="block text-900 font-medium mb-2"
        >
          {t("Title")}
        </label>
        <InputText
          id="title"
          name="title"
          type="text"
          placeholder={t("Title")}
          value={workoutPlanDto.title}
          className="w-full"
          disabled={isReadOnly}
          onChange={(e) =>
            updateWorkoutPlanDto({ [e.target.name]: e.target.value })
          }
        />
      </div>

      <div className="field w-full">
        <label
          htmlFor="userId"
          className="block text-900 font-medium mb-2"
        >
          {t("User")}
        </label>
        <LookupComponent
          controller="users"
          selectedEntityId={workoutPlanDto.userId}
          onChange={(e) => updateWorkoutPlanDto({ userId: e?.id })}
          isEnabled={!isReadOnly}
        />
      </div>

      {/*                                                */}
      {/*       Rule and week, administrators only       */}
      {/*                                                */}

      {canManageRule && (
        <>
          <div className="field w-full">
            <label
              htmlFor="workoutPlanRuleId"
              className="block text-900 font-medium mb-2"
            >
              {t("Rule")}
            </label>
            <LookupComponent
              controller="WorkoutPlanRules"
              selectedEntityId={
                workoutPlanDto.workoutPlanRuleId?.toString() ?? ""
              }
              onChange={(e) => onRuleChange(e?.id ? Number(e.id) : undefined)}
              isEnabled={!isReadOnly}
            />
          </div>

          {workoutPlanDto.workoutPlanRuleId != null && (
            <div className="field w-full">
              <label
                htmlFor="currentWeek"
                className="block text-900 font-medium mb-2"
              >
                {t("Current week")} *
              </label>
              <Dropdown
                inputId="currentWeek"
                value={workoutPlanDto.currentWeek ?? null}
                options={weekOptions}
                onChange={(e) => updateWorkoutPlanDto({ currentWeek: e.value })}
                placeholder={t("Select a value")}
                emptyMessage={t("No data found.")}
                className={isWeekMissing ? "w-full p-invalid" : "w-full"}
                disabled={isReadOnly}
              />
              {isWeekMissing && (
                <small className="block mt-1 p-error">
                  {t("Set the current week for the selected rule")}
                </small>
              )}
            </div>
          )}
        </>
      )}

      <div className="field w-full">
        <label
          htmlFor="description"
          className="block text-900 font-medium mb-2"
        >
          {t("Description")}
        </label>
        <InputTextarea
          id="description"
          rows={8}
          value={workoutPlanDto.description}
          className="w-full"
          disabled={isReadOnly}
          onChange={(e) => updateWorkoutPlanDto({ description: e.target.value })}
        />
      </div>

      <div className="field flex align-items-center gap-2">
        <Checkbox
          inputId="isCircular"
          checked={workoutPlanDto.isCircular}
          disabled={isReadOnly}
          onChange={(e) => updateWorkoutPlanDto({ isCircular: e.checked })}
        />
        <label
          htmlFor="isCircular"
          className="text-900 font-medium"
        >
          {t("Circular")}
        </label>
      </div>
    </div>
  );
}
