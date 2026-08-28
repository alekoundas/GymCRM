import { Button } from "primereact/button";
import { Tag } from "primereact/tag";
import { Message } from "primereact/message";
import { Avatar } from "primereact/avatar";
import { useNavigate } from "react-router-dom";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { TokenService } from "../../services/TokenService";

interface IField {
  isAdminPage: boolean;
  onEdit: () => void;
}

//
//          The plan read as facts. Nothing here is a field - an administrator
//          edits the whole plan in one dialog from the pencil.
//
export default function WorkoutPlanHeaderComponent({
  isAdminPage,
  onEdit,
}: IField) {
  const { t } = useTranslator();
  const navigate = useNavigate();
  const { workoutPlanDto } = useWorkoutPlanStore();

  const weekCount = workoutPlanDto.weekCount ?? 0;
  const currentWeek = workoutPlanDto.currentWeek;

  // Shown wherever an administrator lands, so a trainer can look at exactly
  // what the member sees and fix it without changing pages.
  const canEdit = TokenService.isUserAllowed("WorkoutPlansAdmin_Edit");

  const groupCount = new Set(workoutPlanDto.exercises.map((x) => x.groupNumber))
    .size;

  return (
    <div className="w-full">
      <Button
        label={t("All plans")}
        icon="pi pi-angle-left"
        className="p-button-text p-button-sm p-0 mb-2"
        onClick={() =>
          navigate(
            isAdminPage ? "/administrator/workout-plans" : "/workout-plans",
          )
        }
      />

      {/*                            */}
      {/*       Title and meta       */}
      {/*                            */}

      <div className="flex align-items-start justify-content-between gap-2">
        <h1 className="m-0 text-4xl font-bold line-height-2">
          {workoutPlanDto.title}
        </h1>
        {canEdit && (
          <Button
            icon="pi pi-pencil"
            className="flex-none p-button-rounded p-button-text p-button-secondary"
            tooltip={t("Edit plan")}
            onClick={onEdit}
          />
        )}
      </div>

      <div className="flex flex-wrap align-items-center gap-2 mt-1 text-sm text-color-secondary">
        <span>
          {workoutPlanDto.exercises.length} {t("Exercises")}
        </span>
        <span>·</span>
        <span>
          {groupCount} {t("Groups")}
        </span>
        {workoutPlanDto.isCircular && (
          <Tag
            severity="warning"
            icon="pi pi-replay"
            value={t("Circular")}
          />
        )}
      </div>

      {/*                         */}
      {/*       Current week      */}
      {/*                         */}

      {weekCount > 0 && currentWeek ? (
        <div className="flex align-items-center gap-2 mt-3 p-3 border-1 surface-border border-round">
          <i className="pi pi-replay text-primary" />
          <span className="text-lg font-semibold text-primary">
            {t("Week")} {currentWeek}
          </span>
          <span className="text-color-secondary">
            {t("of")} {weekCount}
          </span>
        </div>
      ) : workoutPlanDto.workoutPlanRuleId ? (
        <div className="mt-3">
          <Tag
            severity="secondary"
            value={t("Never")}
          />
        </div>
      ) : null}

      {(workoutPlanDto.currentWeekMessage?.length ?? 0) > 0 && (
        <div className="flex gap-2 mt-3 p-3 border-1 surface-border border-round">
          <i className="pi pi-megaphone text-primary mt-1" />
          <span className="line-height-3">
            {workoutPlanDto.currentWeekMessage}
          </span>
        </div>
      )}

      {workoutPlanDto.hasIncompleteRecording && (
        <Message
          severity="warn"
          className="w-full mt-3 justify-content-start"
          text={
            t("The recording was left open") +
            ". " +
            t("No duration was recorded") +
            "."
          }
        />
      )}
    </div>
  );
}

//
//          Recordings, the member card and the trainer's notes. Kept apart
//          from the header so the action bar can sit between them.
//
export function WorkoutPlanHeaderFooter({
  onOpenRecordings,
}: {
  onOpenRecordings: () => void;
}) {
  const { t } = useTranslator();
  const { workoutPlanDto } = useWorkoutPlanStore();

  const user = workoutPlanDto.user;
  const initials = user
    ? `${user.firstName?.charAt(0) ?? ""}${user.lastName?.charAt(0) ?? ""}`.toUpperCase()
    : "";

  const formatDate = (value: string | undefined): string => {
    if (!value) return "";
    const date = new Date(value);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  return (
    <div className="w-full">
      <Button
        className="w-full p-button-outlined p-button-secondary mt-2"
        onClick={onOpenRecordings}
      >
        <i className="pi pi-history mr-2" />
        <span className="flex-1 text-center">{t("Recordings")}</span>
        <span className="text-color-secondary">
          {workoutPlanDto.recordingCount}
        </span>
      </Button>

      {user && (
        <div className="flex align-items-center gap-3 mt-3 p-3 border-1 surface-border border-round">
          <Avatar
            image={
              user.profileImage
                ? "data:image/png;base64," + user.profileImage
                : undefined
            }
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="large"
          />
          <div className="min-w-0">
            <div className="font-semibold white-space-normal">
              {user.firstName} {user.lastName}
            </div>
            <div className="text-sm text-color-secondary">
              {t("CreatedOn")} {formatDate(workoutPlanDto.createdOn)}
            </div>
          </div>
        </div>
      )}

      {workoutPlanDto.description.length > 0 && (
        <div className="mt-3 p-3 border-1 surface-border border-round">
          <div className="flex align-items-center gap-2 mb-2">
            <i className="pi pi-align-left text-color-secondary text-sm" />
            <span className="text-xs uppercase text-color-secondary">
              {t("Plan notes")}
            </span>
          </div>
          <div
            className="line-height-3 break-words"
            style={{ whiteSpace: "pre-line" }}
          >
            {workoutPlanDto.description}
          </div>
        </div>
      )}
    </div>
  );
}
