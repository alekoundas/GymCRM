import { useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import GenericDialogComponent from "../../components/core/dialog/GenericDialogComponent";
import { FormMode } from "../../enum/FormMode";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export type WorkoutPlanActivationMode = "DEACTIVATE" | "ACTIVATE";

interface IField {
  plan: WorkoutPlanDto | undefined;
  mode: WorkoutPlanActivationMode;
  isAdminPage: boolean;
  onHide: () => void;
  onDone: () => void;
}

// Deactivating hides a plan from its member. A member can do it to their own plan
// but cannot take it back - only the admin can activate it again - so they are told
// exactly that before they confirm. The admin gets a plain confirmation either way.
export default function WorkoutPlanActivationDialogComponent({
  plan,
  mode,
  isAdminPage,
  onHide,
  onDone,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const [isSaving, setIsSaving] = useState(false);

  const onConfirm = async () => {
    if (!plan) return;

    setIsSaving(true);
    const response = await apiService.setWorkoutPlanActive(plan.id, mode === "ACTIVATE");
    setIsSaving(false);

    if (response) onDone();
  };

  return (
    <GenericDialogComponent
      header={
        !isAdminPage && mode === "DEACTIVATE"
          ? t("Deactivate workout plan")
          : `${t("Are you sure")}?`
      }
      visible={plan !== undefined}
      control={{ showDialog: () => {}, hideDialog: onHide }}
      formMode={FormMode.EDIT}
      footer={
        <Button
          label={mode === "ACTIVATE" ? t("Activate") : t("Deactivate")}
          icon={mode === "ACTIVATE" ? "pi pi-eye" : "pi pi-eye-slash"}
          severity={mode === "ACTIVATE" ? "success" : "danger"}
          loading={isSaving}
          onClick={onConfirm}
        />
      }
    >
      <ActivationContent
        title={plan?.title ?? ""}
        mode={mode}
        isAdminPage={isAdminPage}
      />
    </GenericDialogComponent>
  );
}

// A component of its own: the dialog hands its child DialogChildProps and unwraps a
// plain div to do it.
function ActivationContent({
  title,
  mode,
  isAdminPage,
}: {
  title: string;
  mode: WorkoutPlanActivationMode;
  isAdminPage: boolean;
}) {
  const { t } = useTranslator();

  if (isAdminPage)
    return (
      <p className="m-0">
        <span className="font-semibold">{title}</span>
        {" - "}
        {mode === "ACTIVATE"
          ? t("The member will see this plan again")
          : t("The member will no longer see this plan")}
        .
      </p>
    );

  return (
    <div className="flex flex-column gap-3">
      <p className="m-0">
        <span className="font-semibold">{title}</span>
        {" - "}
        {t("This plan will be removed from your workout plans")}.
      </p>
      <Message
        severity="warn"
        className="w-full justify-content-start"
        text={`${t("You cannot undo this yourself. To use the plan again, ask the gym to activate it for you")}.`}
      />
    </div>
  );
}
