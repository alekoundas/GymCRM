import { FormMode } from "../../enum/FormMode";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import LookupComponent from "../../components/core/dropdown/LookupComponent";
import { useUserStore } from "../../stores/UserStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { UserRoleDto } from "../../model/entities/user-role/UserRoleDto";
import { RoleDto } from "../../model/entities/role/RoleDto";

interface IField extends DialogChildProps {}

export default function UserFormComponent({ formMode }: IField) {
  const { userDto, updateUserDto } = useUserStore();

  return (
    <>
      <div className="flex flex-column align-items-center justify-content-center ">
        <div className="field ">
          <label
            htmlFor="name"
            className="block text-900 font-medium mb-2"
          >
            User Name
          </label>
          <InputText
            id="userName"
            name="userName"
            value={userDto.userName}
            onChange={(x) => updateUserDto({ [x.target.name]: x.target.value })}
            disabled={formMode !== FormMode.EDIT}
          />
        </div>

        <div className="field ">
          <label
            htmlFor="name"
            className="block text-900 font-medium mb-2"
          >
            Email
          </label>
          <InputText
            id="email"
            name="email"
            value={userDto.email}
            onChange={(x) => updateUserDto({ [x.target.name]: x.target.value })}
            disabled={formMode !== FormMode.EDIT}
          />
        </div>

        <div className="field ">
          <label
            htmlFor="roleId"
            className="block text-900 font-medium mb-2"
          >
            Role Name
          </label>
          <LookupComponent
            controller="roles"
            selectedEntityId={userDto.userRoles[0]?.role?.id ?? ""}
            isEnabled={formMode === FormMode.EDIT}
            onChange={(x) =>
              updateUserDto({
                userRoles: x
                  ? [
                      {
                        ...new UserRoleDto(),
                        role: {
                          ...new RoleDto(),
                          id: x.id ?? "",
                          name: x.value ?? "",
                        },
                      },
                    ]
                  : [],
              })
            }
          />
        </div>
      </div>
    </>
  );
}
