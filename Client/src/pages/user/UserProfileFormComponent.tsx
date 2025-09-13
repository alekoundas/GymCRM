import { InputText } from "primereact/inputtext";
import { useUserStore } from "../../stores/UserStore";
import { Button } from "primereact/button";
import { useEffect, useState } from "react";
import ApiService from "../../services/ApiService";
import { UserDto } from "../../model/entities/user/UserDto";

export default function UserProfileFormComponent() {
  const { userDto, updateUserDto } = useUserStore();
  const [editingField, setEditingField] = useState<string | null>(null); // Track which field is being edited
  const [originalValues, setOriginalValues] = useState<Partial<UserDto>>({}); // Store original values per field
  const [formData, setFormData] = useState({
    firstName: userDto.firstName,
    lastName: userDto.lastName,
  });

  // Sync formData when userDto changes
  useEffect(() => {
    setFormData({
      firstName: userDto.firstName,
      lastName: userDto.lastName,
    });
  }, [userDto]);

  // Handle Edit button for a specific field
  const handleEdit = (field: keyof typeof formData) => {
    setEditingField(field);
    setOriginalValues({ [field]: formData[field] });
  };

  // Handle Cancel for a specific field
  const handleCancel = (field: keyof typeof formData) => {
    setFormData({
      ...formData,
      [field]: originalValues[field] ?? userDto[field],
    });
    setEditingField(null);
    setOriginalValues({});
  };

  // Handle Save for a specific field
  const handleSave = async (field: keyof typeof formData) => {
    try {
      const updatedUser = {
        ...userDto,
        [field]: formData[field],
      };
      const response = await ApiService.update<UserDto>(
        "Users",
        updatedUser,
        userDto.id
      );
      if (response) {
        updateUserDto(response); // Update store with backend response
        setEditingField(null);
        setOriginalValues({});
      }
    } catch (error) {
      console.error("Failed to save field:", error);
      // Handle error (e.g., show toast)
    }
  };

  // Handle input change
  const handleChange = (field: keyof typeof formData, value: string) => {
    setFormData({
      ...formData,
      [field]: value,
    });
  };

  return (
    <div className="w-full">
      <div className="field">
        <label
          htmlFor="firstName"
          className="block text-900 font-medium mb-2"
        >
          First Name
        </label>
        <div className="flex align-items-center gap-2">
          <InputText
            id="firstName"
            type="text"
            placeholder="First Name"
            className="w-full mb-3"
            value={formData.firstName}
            onChange={(e) => handleChange("firstName", e.target.value)}
            disabled={editingField !== "firstName"}
          />
          {editingField !== "firstName" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("firstName")}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("firstName")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("firstName")}
              />
            </>
          )}
        </div>
      </div>

      <div className="field">
        <label
          htmlFor="lastName"
          className="block text-900 font-medium mb-2"
        >
          Last Name
        </label>
        <div className="flex align-items-center gap-2">
          <InputText
            id="lastName"
            type="text"
            placeholder="Last Name"
            className="w-full mb-3"
            value={formData.lastName}
            onChange={(e) => handleChange("lastName", e.target.value)}
            disabled={editingField !== "lastName"}
          />
          {editingField !== "lastName" ? (
            <Button
              icon="pi pi-pencil"
              className="p-button-rounded p-button-text p-button-secondary"
              onClick={() => handleEdit("lastName")}
            />
          ) : (
            <>
              <Button
                icon="pi pi-times"
                className="p-button-rounded p-button-text p-button-danger"
                onClick={() => handleCancel("lastName")}
              />
              <Button
                icon="pi pi-check"
                className="p-button-rounded p-button-text p-button-success"
                onClick={() => handleSave("lastName")}
              />
            </>
          )}
        </div>
      </div>

      <div className="field">
        <label
          htmlFor="email"
          className="block text-900 font-medium mb-2"
        >
          Email
        </label>
        <div className="flex align-items-center gap-2">
          <InputText
            id="email"
            type="text"
            placeholder="Email address"
            className="w-full mb-3"
            value={userDto.email}
            disabled={editingField !== "email"}
          />
        </div>
      </div>
    </div>
  );
}
