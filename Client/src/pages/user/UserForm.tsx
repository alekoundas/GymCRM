import { FormMode } from "../../enum/FormMode";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { useUserStore } from "../../stores/UserStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";

interface IField extends DialogChildProps {
  formMode: FormMode;
}

export default function UserForm({ formMode }: IField) {
  const { userDto, updateUserDto } = useUserStore();

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
                onChange={(x) =>
                  updateUserDto({ [x.target.name]: x.target.value })
                }
                disabled
              />
            </div>
            <div className="field">
              <label htmlFor="name">Email</label>
              <InputText
                id="email"
                name="email"
                value={userDto.email}
                onChange={(x) =>
                  updateUserDto({ [x.target.name]: x.target.value })
                }
                disabled
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
                // onCustomChange={isCustomChange}
                onChange={(x) => updateUserDto({ roleId: x })}
              />
            </div>
          </div>
        </form>
      </Card>
    </>
  );
}
