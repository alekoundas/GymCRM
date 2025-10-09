import React, { useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { useApiService } from "../../../services/ApiService";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  id: string | number;
  name: string;
  onAfterRowDeletion: () => void;
  triggerDialogVisibility: (callback: (value: boolean) => void) => void;
}

export default function DeleteDialogComponent({
  id,
  name,
  onAfterRowDeletion,
  triggerDialogVisibility,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const [isVisible, setIsVisible] = useState(false);

  const onDelete = () => {
    apiService.delete("roles", id).then(() => onAfterRowDeletion());
  };

  React.useEffect(() => {
    triggerDialogVisibility((visibility: boolean) => setIsVisible(visibility));
  }, [triggerDialogVisibility]);

  const dialogFooter = () => (
    <React.Fragment>
      <Button
        label={t("Cancel")}
        icon="pi pi-times"
        outlined
        onClick={() => setIsVisible(false)}
      />
      <Button
        label={t("Delete")}
        icon="pi pi-trash"
        onClick={onDelete}
      />
    </React.Fragment>
  );

  return (
    <>
      <Dialog
        visible={isVisible}
        style={{ width: "35%" }}
        header={t("Confirm")}
        modal
        className="p-fluid"
        footer={dialogFooter()}
        onHide={() => setIsVisible(false)}
      >
        <div className="confirmation-content">
          <i
            className="pi pi-exclamation-triangle mr-3"
            style={{ fontSize: "2rem" }}
          />
          <span>
            {t("Are you sure you want to delete ")} {name}?
          </span>
        </div>
      </Dialog>
    </>
  );
}
