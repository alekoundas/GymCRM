import React, { useState } from "react";
import { FormMode } from "../../enum/FormMode";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import ApiService from "../../services/ApiService";
import { ToastService } from "../../services/ToastService";
import { MakerDto } from "../../model/MakerDto";

interface IField {
  data: MakerDto;
  formMode: FormMode;
  // onEnableSaveButton?: () => void;
  // onDisableSaveButton?: () => void;
  onAfterSave: () => void;
  triggerSaveForm?: () => void;
}

export default function MakerForm({
  data,
  formMode,
  // onEnableSaveButton,
  // onDisableSaveButton,
  onAfterSave,
  triggerSaveForm,
}: IField) {
  const [makerDto, setmakerDto] = useState(data);
  const isFirstRender = React.useRef(true);

  //
  // Handle save triggered from parent.
  //
  React.useEffect(() => {
    // Skip the first run
    if (isFirstRender.current) {
      isFirstRender.current = false;
      return;
    }
    if (triggerSaveForm) {
      handleSave();
    }
  }, [triggerSaveForm]);

  const handleSave = () => {
    if (formMode == FormMode.EDIT) {
      ApiService.update("makers", makerDto, makerDto.id)
        .then((response) => {
          if (response) {
            ToastService.showSuccess("");
            return;
          }
          ToastService.showError("");
        })
        .then(onAfterSave);
    } else {
      ApiService.create("makers", makerDto)
        .then((response) => {
          if (response) {
            ToastService.showSuccess("");
            return;
          }
          ToastService.showError("");
        })
        .then(onAfterSave);
    }
  };

  const handleChange = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    makerDto[name] = value;
    setmakerDto({ ...makerDto });
  };

  // Load initial data.
  React.useEffect(() => {
    if (formMode === FormMode.EDIT || formMode === FormMode.VIEW) {
      ApiService.get<MakerDto>("makers", makerDto.id).then((result) => {
        if (result) {
          setmakerDto({ ...result });
        }
      });
    }
  }, []);

  return (
    <>
      <Card title="Makers Form ">
        <form>
          <div className="flex align-items-center justify-content-center">
            <div className="field">
              <label htmlFor="name">Role Name</label>
              <InputText
                id="name"
                name="name"
                value={makerDto.name}
                onChange={handleChange}
                disabled={formMode !== FormMode.ADD}
              />
            </div>
          </div>
        </form>
      </Card>
    </>
  );
}
