import { InputText } from "primereact/inputtext";
import { useUserStore } from "../../stores/UserStore";

export default function UserProfileFormComponent() {
  const { userDto, updateUserDto } = useUserStore();

  return (
    // <>
    <div className="w-full">
      <div className="field ">
        <label
          htmlFor="firstName"
          className="block text-900 font-medium mb-2"
        >
          First Name
        </label>
        <InputText
          id="firstName"
          name="firstName"
          type="text"
          placeholder="First Name"
          className="w-full mb-3"
          value={userDto.firstName}
          onChange={(x) => updateUserDto({ firstName: x.target.value })}
        />
      </div>

      <div className="field ">
        <label
          htmlFor="lastName"
          className="block text-900 font-medium mb-2"
        >
          Last Name
        </label>
        <InputText
          id="lastName"
          name="lastName"
          type="text"
          placeholder="Last Name"
          className="w-full mb-3"
          value={userDto.lastName}
          onChange={(x) => updateUserDto({ lastName: x.target.value })}
        />
      </div>

      <div className="field ">
        <label
          htmlFor="email"
          className="block text-900 font-medium mb-2"
        >
          Email
        </label>
        <InputText
          id="email"
          name="email"
          type="text"
          placeholder="Email address"
          className="w-full mb-3"
          value={userDto.email}
          onChange={(x) => updateUserDto({ email: x.target.value })}
        />
      </div>
    </div>
    // </>
  );
}
