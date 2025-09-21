import { FormMode } from "../../enum/FormMode";
import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useMailStore } from "../../stores/MailStore";
import { AutoComplete } from "primereact/autocomplete";
import { useEffect, useState } from "react";
import { UserDto } from "../../model/entities/user/UserDto";
import { Chip } from "primereact/chip";

interface IField extends DialogChildProps {}

export default function MailSendFormComponent({ formMode }: IField) {
  const { mailDto, updateMailDto } = useMailStore();
  const [filteredUsers, setFilteredUsers] = useState<UserDto[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<UserDto[]>(
    mailDto.users || []
  );

  // Mock user fetch (replace with your actual API or store call)
  const allUsers: UserDto[] = [
    // Example users; replace with your data source
    {
      id: "1",
      userName: "johndoe",
      email: "john.doe@example.com",
      firstName: "John",
      lastName: "Doe",
      roleId: "user",
      profileImage: "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAAAAAAAD...", // Example base64
    },
    {
      id: "2",
      userName: "janedoe",
      email: "jane.doe@example.com",
      firstName: "Jane",
      lastName: "Doe",
      roleId: "user",
      profileImage: null,
    },
  ];

  // Filter users for AutoComplete suggestions
  const searchUsers = (event: { query: string }) => {
    const query = event.query.toLowerCase();
    const filtered = allUsers.filter(
      (user) =>
        user.firstName.toLowerCase().includes(query) ||
        user.lastName.toLowerCase().includes(query) ||
        user.email.toLowerCase().includes(query)
    );
    setFilteredUsers(filtered);
  };

  // Handle user selection
  const handleUserSelect = (e: { value: UserDto[] }) => {
    const newUsers = e.value;
    setSelectedUsers(newUsers);
    updateMailDto({
      userIds: newUsers.map((user) => user.id),
      users: newUsers,
    });
  };

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
    return (
      <Chip
        label={`${user.firstName} ${user.lastName}`}
        image={user.profileImage || undefined}
        removable
        onRemove={() => {
          const updatedUsers = selectedUsers.filter((u) => u.id !== user.id);
          setSelectedUsers(updatedUsers);
          updateMailDto({
            userIds: updatedUsers.map((u) => u.id),
            users: updatedUsers,
          });
        }}
        className="mr-2"
      />
    );
  };

  // Sync selected users with mailDto on mount
  useEffect(() => {
    setSelectedUsers(mailDto.users || []);
  }, [mailDto.users]);

  return (
    <div className="flex flex-column gap-3">
      <div className="field">
        <label htmlFor="recipients">Recipients</label>
        <AutoComplete
          id="recipients"
          multiple
          value={selectedUsers}
          suggestions={filteredUsers}
          completeMethod={searchUsers}
          field="email" // Display email in the input
          itemTemplate={userTemplate}
          onChange={handleUserSelect}
          disabled={formMode !== FormMode.ADD}
          className="w-full"
          inputClassName="w-full"
        />
        <div className="flex flex-wrap gap-2 mt-2">
          {selectedUsers.map((user) => chipTemplate(user))}
        </div>
      </div>
      <div className="field">
        <label htmlFor="subject">Subject</label>
        <InputText
          id="subject"
          name="subject"
          value={mailDto.subject}
          onChange={(e) => updateMailDto({ [e.target.name]: e.target.value })}
          disabled={formMode !== FormMode.ADD}
          className="w-full"
        />
      </div>
      <div className="field">
        <label htmlFor="body">Body</label>
        <InputTextarea
          id="body"
          name="body"
          value={mailDto.body}
          onChange={(e) => updateMailDto({ [e.target.name]: e.target.value })}
          disabled={formMode !== FormMode.ADD}
          rows={5}
          className="w-full"
        />
      </div>
    </div>
  );
}
