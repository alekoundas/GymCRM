import React, { ReactNode, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";

interface IField {
  children: ReactNode;
  triggerDialogVisibility: (callback: (value: boolean) => void) => void;
}

export default function ViewDialogComponent({
  children,
  triggerDialogVisibility,
}: IField) {
  const [isVisible, setIsVisible] = useState(false);

  React.useEffect(() => {
    triggerDialogVisibility((newValue: boolean) => setIsVisible(newValue));
  }, [triggerDialogVisibility]);

  const dialogFooter = () => (
    <React.Fragment>
      <Button
        label="Close"
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
        header="View"
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
