import React, { ReactNode } from "react";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import "primereact/resources/themes/saga-blue/theme.css";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";

// Interface for child component props
interface DialogChildProps {
  showdialog?: () => void;
  hidedialog?: () => void;
}

export interface DialogControl {
  showdialog: () => void;
  hidedialog: () => void;
}

interface DialogComponentProps {
  header?: string;
  visible: boolean;
  control: DialogControl;
  children: ReactNode;
  onSave?: () => void;
}

const DialogComponent: React.FC<DialogComponentProps> = ({
  header = "Dialog",
  visible,
  control,
  children,
  onSave,
}) => {
  const footer = (
    <div>
      <Button
        label="Cancel"
        icon="pi pi-times"
        onClick={control.hidedialog}
        className="p-button-text"
      />
      {onSave && (
        <Button
          label="Save"
          icon="pi pi-check"
          onClick={onSave}
          autoFocus
        />
      )}
    </div>
  );

  return (
    <Dialog
      header={header}
      visible={visible}
      style={{ width: "50vw" }}
      footer={footer}
      onHide={control.hidedialog}
      draggable={false}
      resizable={false}
    >
      {React.Children.map(children, (child) =>
        React.isValidElement<DialogChildProps>(child)
          ? React.cloneElement(child, {
              showdialog: control.showdialog,
              hidedialog: control.hidedialog,
            })
          : child
      )}
    </Dialog>
  );
};

export default DialogComponent;
