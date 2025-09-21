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

  // Filter users for AutoComplete suggestions
  // const searchUsers = (event: { query: string }) => {
  //   const query = event.query.toLowerCase();
  //   const filtered = allUsers.filter(
  //     (user) =>
  //       user.firstName.toLowerCase().includes(query) ||
  //       user.lastName.toLowerCase().includes(query) ||
  //       user.email.toLowerCase().includes(query)
  //   );
  //   setFilteredUsers(filtered);
  // };

  // Handle user selection

  // Custom suggestion template for AutoComplete
  const userTemplate = (user: UserDto) => {
    return (
      <div className="flex align-items-center">
        {user.profileImage ? (
          <img
            src={user.profileImage}
            alt={`${user.firstName} ${user.lastName}`}
            style={{
              width: "24px",
              height: "24px",
              borderRadius: "50%",
              marginRight: "8px",
            }}
          />
        ) : (
          <i
            className="pi pi-user"
            style={{ marginRight: "8px" }}
          />
        )}
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
    return (
      <p className="m-0 p-0">
        <Avatar
          image={user.profileImage ?? ""}
          label={user.profileImage ? undefined : initials}
          shape="circle"
          size="normal"
          style={{
            backgroundColor: user.profileImage
              ? "transparent"
              : "var(--primary-color)",
            color: user.profileImage ? "transparent" : "var(--surface-a)",
          }}
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
