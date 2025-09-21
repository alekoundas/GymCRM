import { FormMode } from "../../enum/FormMode";
import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { UserDto } from "../../model/entities/user/UserDto";
import { Chip, ChipRemoveEvent } from "primereact/chip";
import { InputTextarea } from "primereact/inputtextarea";
import { MailSendDto } from "../../model/entities/mail/MailSendDto";
import {
  VirtualScrollerLazyEvent,
  VirtualScrollerProps,
} from "primereact/virtualscroller";
import ApiService from "../../services/ApiService";
import AutoCompleteComponent from "../../components/core/auto-complete/AutoCompleteComponent";
import { useState } from "react";
import { Avatar } from "primereact/avatar";

interface IField extends DialogChildProps {}

export default function MailSendFormComponent({ formMode }: IField) {
  const [mailSendDto, setMailSendDto] = useState<MailSendDto>(
    new MailSendDto()
  );
  const [filteredUsers, setFilteredUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(false);

  const [selectedUsers, setSelectedUsers] = useState<UserDto[]>(
    mailSendDto.users || []
  );

  const getDisplayImageSrc = (
    profileImage: string | undefined
  ): string | undefined => {
    if (!profileImage) return undefined;
    if (profileImage.startsWith("data:")) return profileImage;
    return `data:image/png;base64,${profileImage}`;
  };

  // Custom suggestion template for AutoComplete
  const userTemplate = (user: UserDto) => {
    const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
      0
    )}`.toUpperCase();
    const imageSrc = getDisplayImageSrc(user.profileImage ?? "");

    const avatar = (
      <Avatar
        image={imageSrc ?? ""}
        label={imageSrc ? undefined : initials}
        shape="circle"
        size="normal"
        className=" mr-2 "
      />
    );

    return (
      <div className="flex">
        {user.profileImage ? avatar : <i className="pi pi-user mr-2" />}
        <div>
          {user.firstName} {user.lastName} ({user.email})
        </div>
      </div>
    );
  };

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto) => {
    const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
      0
    )}`.toUpperCase();
    const imageSrc = getDisplayImageSrc(user.profileImage ?? "");
    return (
      <p className="flex m-0 p-0 align-items-center">
        <Avatar
          image={imageSrc ?? ""}
          label={imageSrc ? undefined : initials}
          shape="circle"
          size="normal"
          className=" mr-2 "
        />
        {" " +
          user.firstName[0].toUpperCase() +
          "." +
          " " +
          user.lastName[0].toUpperCase() +
          user.lastName.slice(1, user.lastName.length) +
          "(" +
          user.email +
          ")"}
      </p>
    );
  };

  return (
    <div className="flex flex-column gap-3">
      <div className="field ">
        <label htmlFor="recipients">Recipients</label>
        <AutoCompleteComponent
          controller="users"
          isEnabled={true}
          itemTemplate={userTemplate}
          selectedItemTemplate={chipTemplate}
          onChange={(x) =>
            setMailSendDto({ ...mailSendDto, usersIds: x.map((y) => y.id) })
          }
        />
      </div>
      <div className="field">
        <label htmlFor="subject">Subject</label>
        <InputText
          id="subject"
          name="subject"
          value={mailSendDto.subject}
          onChange={(e) =>
            setMailSendDto({ ...mailSendDto, [e.target.name]: e.target.value })
          }
          disabled={formMode === FormMode.VIEW}
          className="w-full"
        />
      </div>
      <div className="field">
        <label htmlFor="body">Body</label>
        <InputTextarea
          id="body"
          name="body"
          value={mailSendDto.body}
          onChange={(e) =>
            setMailSendDto({ ...mailSendDto, [e.target.name]: e.target.value })
          }
          disabled={formMode === FormMode.VIEW}
          rows={5}
          className="w-full"
        />
      </div>
    </div>
  );
}
