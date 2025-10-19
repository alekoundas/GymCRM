import { Card } from "primereact/card";
import { useTranslator } from "../../services/TranslatorService";
import UserStatusGridComponent from "./UserStatusGridComponent";

export default function UserStatusesPage() {
  const { t } = useTranslator();

  return (
    <>
      <Card title={t("User Statuses")}>
        <div className="card">
          <UserStatusGridComponent />
        </div>
      </Card>
    </>
  );
}
