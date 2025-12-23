import { useEffect, useState } from "react";
import { Card } from "primereact/card";
import { useMailStore } from "../../stores/MailStore";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import MailSendFormComponent from "./MailSendFormComponent";
import { useNavigate } from "react-router-dom";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function MailSendPage() {
  const { t } = useTranslator();
  const navigate = useNavigate();
  const apiService = useApiService();

  const { mailSendDto, resetMailSendDto } = useMailStore();
  const [isInfoDialogVisible, setInfoDialogVisible] = useState(false); // Dialog visibility

  // useEffect(() => resetMailSendDto(), []); // Reset form on load.

  const onSend = async () => {
    const response = await apiService.emailSend(mailSendDto);
    if (response) {
      navigate("/administrator/emails");
    }
  };

  const footer = () => {
    return (
      <div
        style={{
          display: "flex",
          justifyContent: "flex-end",
          alignItems: "center",
          width: "100%",
        }}
      >
        <Button
          label="Send"
          icon="pi pi-check"
          onClick={onSend}
          autoFocus
        />
      </div>
    );
  };

  const header = () => {
    return (
      <div className="flex justify-content-between align-items-center p-3">
        <div className="flex flex-column gap-1">
          <h2 className="m-0">{t("Send new email")} </h2>
          <p className="m-0 text-gray-600">
            {t("Send one email to each recipient")}
          </p>
        </div>
        <Button
          label=""
          icon="pi pi-info-circle"
          onClick={() => setInfoDialogVisible(true)}
          className="p-button-text"
        />
      </div>
    );
  };

  return (
    <>
      <Card
        footer={footer}
        header={header}
      >
        <MailSendFormComponent />
      </Card>

      {/*                                       */}
      {/*             Info Dialog               */}
      {/*                                       */}
      <Dialog
        header={t("Send Email to multiple users")}
        visible={isInfoDialogVisible}
        style={{ width: "50vw" }}
        onHide={() => {
          if (!isInfoDialogVisible) return;
          setInfoDialogVisible(false);
        }}
      >
        <p>{t("Create a new email and send it to multiple users")}.</p>
        <ul>
          <li>
            {t("A new email will be created and sent for each recipient")}.
          </li>
          <li>
            {t(
              "The email body will be converted to HTML and inserted into the email content. Note that different email providers may render HTML differently, which could lead to inconsistencies in appearance."
            )}
          </li>
        </ul>
      </Dialog>
    </>
  );
}
