// src/components/UserProfile.tsx
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { FileUpload } from "primereact/fileupload";
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
  const { userDto, updateUserDto, setUserDto } = useUserStore();

  const [loading, setLoading] = useState(true);
  const [isImageHovered, setIsImageHovered] = useState(false);

  // Load initial data.
  useEffect(() => {
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
  const handleImageUpload = async (event: any) => {
    const file = event.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append("image", file);
  };

  const renderProfileImage = () => {
    const initials = `${userDto?.firstName?.charAt(
      0
    )}${userDto?.lastName?.charAt(0)}`.toUpperCase();
    return (
      <div
        style={{ position: "relative", display: "inline-block" }}
        onMouseEnter={() => setIsImageHovered(true)}
        onMouseLeave={() => setIsImageHovered(false)}
      >
        <Avatar
          image={userDto.imageUrl}
          label={userDto.imageUrl ? undefined : initials}
          shape="circle"
          size="xlarge"
          style={{
            width: "80px",
            height: "80px",
            backgroundColor: userDto.imageUrl
              ? "transparent"
              : "var(--primary-color)",
            color: userDto.imageUrl ? "transparent" : "var(--surface-a)",
            fontSize: "2rem",
            transition: "opacity 0.3s",
            opacity: isImageHovered && userDto.imageUrl ? 0.7 : 1,
          }}
        />
        {isImageHovered && (
          <FileUpload
            mode="basic"
            name="image"
            accept="image/*"
            maxFileSize={5000000}
            customUpload
            uploadHandler={handleImageUpload}
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
