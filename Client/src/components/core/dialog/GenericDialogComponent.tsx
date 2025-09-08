import React, { ReactNode, useState } from "react";
import { Dialog, DialogProps } from "primereact/dialog";
import { Button } from "primereact/button";
import "primereact/resources/themes/saga-blue/theme.css";
import "primereact/resources/primereact.min.css";
import "primeicons/primeicons.css";
import { FormMode } from "../../../enum/FormMode";

// Interface for child component props
export interface DialogChildProps {
  showDialog?: () => void;
  hideDialog?: () => void;
  toggleDialogSave?: (isEnabled: React.SetStateAction<boolean>) => void;
  formMode?: FormMode;
}

export interface DialogControl {
  showDialog: () => void;
  hideDialog: () => void;
}

interface DialogComponentProps {
  header?: string;
  formMode: FormMode;
  visible: boolean;
  control: DialogControl;
  children: ReactNode;
  onSave?: () => Promise<void>;
  onDelete?: () => void;
}

const DialogComponent: React.FC<DialogComponentProps> = ({
  header = "Dialog",
  formMode = FormMode.EDIT,
  visible,
  control,
  children,
  onSave,
  onDelete,
}) => {
  const [isSaveEnabled, setIsSaveEnabled] = useState<boolean>(true);

  const handleSave = () => {
    setIsSaveEnabled(false);
    if (onSave) {
      onSave().finally(() => setIsSaveEnabled(true));
    }
  };

  const footer = () => {
    if (formMode === FormMode.VIEW)
      return (
        <div>
          <Button
            label="Cancel"
            icon="pi pi-times"
            onClick={control.hideDialog}
            className="p-button-text"
          />
        </div>
      );

    if (formMode === FormMode.ADD || formMode === FormMode.EDIT)
      return (
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
              onClick={handleSave}
              disabled={!isSaveEnabled}
              autoFocus
            />
          )}
        </div>
      );

    if (formMode === FormMode.DELETE)
      return (
        <div>
          <Button
            label="Cancel"
            icon="pi pi-times"
            onClick={control.hideDialog}
            className="p-button-text"
          />
          {onDelete && (
            <Button
              label="Delete"
              icon="pi pi-danger"
              severity="danger"
              onClick={onDelete}
              // disabled={!isSaveEnabled}
              autoFocus
            />
          )}
        </div>
      );
  };

  // Recursively process children to pass props to components that accept DialogChildProps
  const renderChildrenWithProps = (child: ReactNode): ReactNode => {
    if (React.isValidElement<DialogChildProps>(child)) {
      // Pass DialogChildProps to the clildren.
      if (child.type !== "div") {
        return React.cloneElement(child, {
          showDialog: control.showDialog,
          hideDialog: control.hideDialog,
          toggleDialogSave: setIsSaveEnabled,
          formMode: formMode,
        });
      }

      // Pass DialogChildProps to the clildren of a 'div'.
      if (child.type === "div") {
        const childProps = child.props as { className?: string; children?: [] };
        if (childProps.children) {
          // children = 2 and above
          if (childProps.children.length > 1) {
            return childProps.children.map((x) =>
              React.cloneElement(x, {
                showDialog: control.showDialog,
                hideDialog: control.hideDialog,
                toggleDialogSave: setIsSaveEnabled,
                formMode: formMode,
              })
            );
          } else {
            // children = 1
            var schildProps = child.props as {
              className?: string;
              children?: any;
            };

            if (schildProps.children.type === "p") return child;

            return React.cloneElement(schildProps.children, {
              showDialog: control.showDialog,
              hideDialog: control.hideDialog,
              toggleDialogSave: setIsSaveEnabled,
              formMode: formMode,
            });
          }
        }
      }
    }
    return child;
  };

  return (
    <Dialog
      header={header}
      visible={visible}
      style={{ width: "70vw" }}
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
