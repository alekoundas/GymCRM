import React, { useState } from "react";
import { FormMode } from "../../enum/FormMode";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import ApiService from "../../services/ApiService";
import { ToastService } from "../../services/ToastService";
import { UserDto } from "../../model/UserDto";
import LookupComponent from "../../components/dropdown/LookupComponent";

interface IField {
  parentUserDto: UserDto;
  formMode: FormMode;
  onEnableSaveButton?: () => void;
  onDisableSaveButton?: () => void;
  onAfterSave: () => void;
  triggerSaveForm?: () => void;
}

export default function UserForm({
  parentUserDto,
  formMode,
  onAfterSave,
  triggerSaveForm,
  onDisableSaveButton,
  onEnableSaveButton,
}: IField) {
  const [userDto, setUserDto] = useState(parentUserDto);
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
      ApiService.update("users", userDto, userDto.id)
        .then((response) => {
          if (response) {
            ToastService.showSuccess("");
            return;
          }
          ToastService.showError("");
        })
        .then(onAfterSave);
    } else {
      ApiService.create("users", userDto)
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

  const handlLookupChange = (id: string) => {
    userDto.roleId = id;
    setUserDto({ ...userDto });
  };

  const handleChange = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    userDto[name] = value;
    setUserDto({ ...userDto });
  };
  const isCustomChange = (isCustom: boolean) => {
    if (isCustom && onDisableSaveButton) onDisableSaveButton();
    if (!isCustom && onEnableSaveButton) onEnableSaveButton();
  };

  // Load initial data.
  React.useEffect(() => {
    if (formMode === FormMode.EDIT || formMode === FormMode.VIEW) {
      ApiService.get<UserDto>("users", userDto.id).then((result) => {
        if (result) {
          setUserDto({ ...result });
        }
      });
    }
  }, []);

  return (
    <>
      <Card title="User">
        <form>
          <div className="flex align-items-center justify-content-center">
            <div className="field">
              <label htmlFor="name">User Name</label>
              <InputText
                id="userName"
                name="userName"
                value={userDto.userName}
                onChange={handleChange}
                // disabled={
                //   formMode !== FormMode.ADD && formMode !== FormMode.EDIT
                // }
                disabled
              />
            </div>
            <div className="field">
              <label htmlFor="name">Email</label>
              <InputText
                id="email"
                name="email"
                value={userDto.email}
                onChange={handleChange}
                disabled
                // disabled={
                //   formMode !== FormMode.ADD && formMode !== FormMode.EDIT
                // }
              />
            </div>
            <div className="field">
              <label htmlFor="roleId">Role Name</label>
              <LookupComponent
                controller="roles"
                idValue={userDto.roleId}
                isEditable={true}
                isEnabled={formMode === FormMode.EDIT}
                allowCustom={false}
                onCustomChange={isCustomChange}
                onChange={handlLookupChange}
              />
            </div>
          </div>
        </form>
      </Card>
    </>
  );
}
