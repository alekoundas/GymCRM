import React, { ReactElement, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  children: ReactElement;
  onSaveButtonClick: () => void;
  triggerDialogVisibility: (callback: (value: boolean) => void) => void;
  triggerSaveDisable?: (callback: (value: boolean) => void) => void;
  triggerSaveEnable?: (callback: (value: boolean) => void) => void;
}

export default function AddDialogComponent({
  children,
  onSaveButtonClick,
  triggerDialogVisibility,
  triggerSaveDisable,
  triggerSaveEnable,
}: IField) {
  const { t } = useTranslator();
  const [isVisible, setIsVisible] = useState(false);
  const [isEnabled, setIsEnabled] = useState(true);

  React.useEffect(() => {
    triggerDialogVisibility((value: boolean) => setIsVisible(value));
  }, [triggerDialogVisibility]);

  React.useEffect(() => {
    if (triggerSaveDisable)
      triggerSaveDisable((value: boolean) => setIsEnabled(value));
  }, [triggerSaveDisable, triggerSaveEnable]);

  const dialogFooter = () => (
    <React.Fragment>
      <Button
        label={t("Cancel")}
        icon="pi pi-times"
        outlined
        onClick={() => setIsVisible(false)}
      />
      <Button
        label={t("Save")}
        icon="pi pi-check"
        onClick={() => {
          onSaveButtonClick();
        }}
        disabled={!isEnabled}
      />
    </React.Fragment>
  );

  return (
    <>
      <Dialog
        visible={isVisible}
        style={{ width: "45%" }}
        header={t("Add")}
        modal
        className="p-fluid"
        footer={dialogFooter()}
        onHide={() => setIsVisible(false)}
      >
        {children}
      </Dialog>
    </>
  );
}
