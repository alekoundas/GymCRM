// src/components/UserProfile.tsx
import { FileUpload, FileUploadHandlerEvent } from "primereact/fileupload";
import { UserDto } from "../../model/entities/user/UserDto";
import ApiService from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import { Card } from "primereact/card";
import { Avatar } from "primereact/avatar";
import { useEffect, useState } from "react";
import { useUserStore } from "../../stores/UserStore";
import UserProfileFormComponent from "./UserProfileFormComponent";
import PhoneNumberGridComponent from "../phone-number/PhoneNumberGridComponent";
import UserProfileTimeslotsComponent from "./UserProfileTimeslotsComponent";

export default function UserProfilePage() {
  const { userDto, updateUserDto, setUserDto, resetUserDto } = useUserStore();

  const [loading, setLoading] = useState(true);
  const [isImageHovered, setIsImageHovered] = useState(false);
  const [isImageUploadSelected, setIsImageUploadSelected] = useState(false);

  // Load initial data.
  useEffect(() => {
    resetUserDto();
    const loadUser = async () => {
      const userId = TokenService.getUserId();
      if (userId) {
        const response = await ApiService.get<UserDto>("Users", userId);
        setUserDto(response as UserDto);
      }

      setLoading(false);
    };

    loadUser();
  }, []);

  // Handle image upload
  const handleImageUpload = async (event: FileUploadHandlerEvent) => {
    const file = event.files[0];
    if (!file) return;

    try {
      // Convert file to Base64 for backend
      const reader = new FileReader();
      reader.onload = async () => {
        const base64Image = reader.result as string;

        // Update user with new image
        userDto.profileImage = base64Image;
        const response = await ApiService.update<UserDto>(
          "Users",
          userDto,
          userDto.id
        );
        if (response) {
          updateUserDto(response);
        }
      };
      reader.readAsDataURL(file);
    } catch (error) {
      console.error("Image upload failed:", error);
      // Handle error (e.g., show toast)
    }

    setIsImageUploadSelected(false);
  };

  const renderProfileImage = () => {
    if (userDto?.id) {
      const initials = `${userDto.firstName.charAt(0)}${userDto.lastName.charAt(
        0
      )}`.toUpperCase();
      return (
        <div
          style={{ position: "relative", display: "inline-block" }}
          onMouseEnter={() => setIsImageHovered(true)}
          onMouseLeave={() => setIsImageHovered(false)}
        >
          <Avatar
            image={userDto.profileImage ?? ""}
            label={userDto.profileImage ? undefined : initials}
            shape="circle"
            size="xlarge"
            style={{
              width: "80px",
              height: "80px",
              backgroundColor: userDto.profileImage
                ? "transparent"
                : "var(--primary-color)",
              color: userDto.profileImage ? "transparent" : "var(--surface-a)",
              fontSize: "2rem",
              transition: "opacity 0.3s",
              opacity: isImageHovered && userDto.profileImage ? 0.7 : 1,
            }}
          />
          {(isImageHovered || isImageUploadSelected) && (
            <FileUpload
              mode="basic"
              name="image"
              accept="image/*"
              maxFileSize={5000000}
              customUpload
              uploadHandler={handleImageUpload}
              onSelect={() => setIsImageUploadSelected(true)}
              chooseLabel="Change Image"
              style={{
                position: "absolute",
                top: "50%",
                left: "50%",
                transform: "translate(-50%, -50%)",
                fontSize: "0.875rem",
                zIndex: 1,
              }}
            />
          )}
        </div>
      );
    }
  };

  return (
    // <div className="flex align-items-center justify-content-center">
    <div className="grid">
      <div className="col-12 md:col-5 ">
        <Card
          className="p-4 "
          style={{
            boxShadow: "0 4px 8px rgba(0, 0, 0, 0.1)",
          }}
          header={
            <div className="flex align-items-center mb-5">
              <div className="mr-5">{renderProfileImage()}</div>
              <div>
                <h2 className="m-0">
                  {userDto.firstName} {userDto.lastName}
                </h2>
                <p className="text-color-secondary m-0">{userDto.email}</p>
              </div>
            </div>
          }
        >
          <UserProfileFormComponent />
        </Card>
      </div>

      <div className=" col-12 md:col-7">
        <Card
          className="p-4 "
          title="Phone Numbers"
          style={{
            boxShadow: "0 4px 8px rgba(0, 0, 0, 0.1)",
          }}
        >
          <PhoneNumberGridComponent />
        </Card>
      </div>

      <div className="col-12">
        <Card
          className="p-4 "
          title="Upcoming Train Groups"
          style={{
            boxShadow: "0 4px 8px rgba(0, 0, 0, 0.1)",
          }}
        >
          <UserProfileTimeslotsComponent />
        </Card>
      </div>
    </div>
  );
}
