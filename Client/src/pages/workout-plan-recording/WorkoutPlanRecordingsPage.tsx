import { Card } from "primereact/card";
import { useLocation } from "react-router-dom";
import { useTranslator } from "../../services/TranslatorService";
import WorkoutPlanRecordingGridComponent from "./WorkoutPlanRecordingGridComponent";

export default function WorkoutPlanRecordingsPage() {
  const { t } = useTranslator();
  const location = useLocation();
  const isAdminPage = location.pathname.includes("/administrator");

  return (
    <>
      <Card title={t("Recordings")}>
        <div className="card">
          <WorkoutPlanRecordingGridComponent
            isAdminPage={isAdminPage}
            isUrlStateEnabled
          />
        </div>
      </Card>
    </>
  );
}
