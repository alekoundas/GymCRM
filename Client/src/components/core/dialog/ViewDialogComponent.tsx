import React, { ReactNode, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  children: ReactNode;
  triggerDialogVisibility: (callback: (value: boolean) => void) => void;
}

export default function ViewDialogComponent({
  children,
  triggerDialogVisibility,
}: IField) {
  const { t } = useTranslator();
  const [isVisible, setIsVisible] = useState(false);

  React.useEffect(() => {
    triggerDialogVisibility((newValue: boolean) => setIsVisible(newValue));
  }, [triggerDialogVisibility]);

  const dialogFooter = () => (
    <React.Fragment>
      <Button
        label={t("Close")}
        icon="pi pi-times"
        outlined
        onClick={() => setIsVisible(false)}
      />
    </React.Fragment>
  );

  return (
    <>
      <Dialog
        visible={isVisible}
        style={{ width: "50%" }}
        header={t("View")}
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
