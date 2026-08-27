import { Card } from "primereact/card";
import { useTranslator } from "../../services/TranslatorService";
import WorkoutPlanRuleGridComponent from "./WorkoutPlanRuleGridComponent";

export default function WorkoutPlanRulesPage() {
  const { t } = useTranslator();

  return (
    <>
      <Card title={t("Workout plan rules")}>
        <div className="card">
          <WorkoutPlanRuleGridComponent />
        </div>
      </Card>
    </>
  );
}
