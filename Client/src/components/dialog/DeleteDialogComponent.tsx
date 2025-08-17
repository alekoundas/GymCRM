import React, { useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import ApiService from "../../services/ApiService";

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
  const [isVisible, setIsVisible] = useState(false);

  const onDelete = () => {
    ApiService.delete("roles", id).then(() => onAfterRowDeletion());
  };

  React.useEffect(() => {
    triggerDialogVisibility((visibility: boolean) => setIsVisible(visibility));
  }, [triggerDialogVisibility]);

  const dialogFooter = () => (
    <React.Fragment>
      <Button
        label="Cancel"
        icon="pi pi-times"
        outlined
        onClick={() => setIsVisible(false)}
      />
      <Button
        label="Delete"
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
        header="Confirm"
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
          <span>Are you sure you want to delete {name}?</span>
        </div>
      </Dialog>
    </>
  );
}
