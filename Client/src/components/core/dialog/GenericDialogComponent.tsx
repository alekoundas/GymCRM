import React, { ReactNode, useState } from "react";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import "primereact/resources/themes/saga-blue/theme.css";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";

// Interface for child component props
export interface DialogChildProps {
  showDialog?: () => void;
  hideDialog?: () => void;
  toggleDialogSave?: (isEnabled: React.SetStateAction<boolean>) => void;
}

export interface DialogControl {
  showDialog: () => void;
  hideDialog: () => void;
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
  const [isSaveEnabled, setIsSaveEnabled] = useState<boolean>(true);

  const footer = (
    <div>
      <Button
        label="Cancel"
        icon="pi pi-times"
        onClick={control.hideDialog}
        className="p-button-text"
      />
      {onSave && (
        <Button
          label="Save"
          icon="pi pi-check"
          onClick={onSave}
          disabled={!isSaveEnabled}
          autoFocus
        />
      )}
    </div>
  );

  // Recursively process children to pass props to components that accept DialogChildProps
  const renderChildrenWithProps = (child: ReactNode): ReactNode => {
    if (React.isValidElement<DialogChildProps>(child)) {
      // Pass DialogChildProps to the clildren.
      if (child.type !== "div") {
        return React.cloneElement(child, {
          showDialog: control.showDialog,
          hideDialog: control.hideDialog,
          toggleDialogSave: setIsSaveEnabled,
        });
      }

      // Pass DialogChildProps to the clildren of a 'div'.
      if (child.type === "div") {
        const childProps = child.props as { className?: string; children?: [] };
        if (childProps.children) {
          return childProps.children.map((x) =>
            React.cloneElement(x, {
              showDialog: control.showDialog,
              hideDialog: control.hideDialog,
              toggleDialogSave: setIsSaveEnabled,
            })
          );
        }
      }
    }
    return child;
  };

  return (
    <Dialog
      header={header}
      visible={visible}
      style={{ width: "50vw" }}
      footer={footer}
      onHide={control.hideDialog}
      draggable={false}
      resizable={false}
    >
      {React.Children.map(children, renderChildrenWithProps)}
      {/* {React.Children.map(children, (child) =>
        React.isValidElement<DialogChildProps>(child)
          ? React.cloneElement(child, {
              showDialog: control.showDialog,
              hideDialog: control.hideDialog,
              toggleDialogSave: setIsSaveEnabled,
            })
          : child
      )} */}
    </Dialog>
  );
};

export default DialogComponent;
