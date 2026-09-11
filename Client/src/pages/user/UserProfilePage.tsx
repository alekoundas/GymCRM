// src/components/UserProfile.tsx
import { FileUpload, FileUploadHandlerEvent } from "primereact/fileupload";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import { UserDto } from "../../model/entities/user/UserDto";
import { TokenService } from "../../services/TokenService";
import { Card } from "primereact/card";
import { Avatar } from "primereact/avatar";
import { useEffect, useRef, useState } from "react";
import { useUserStore } from "../../stores/UserStore";
import UserProfileFormComponent from "./UserProfileFormComponent";
import PhoneNumberGridComponent from "../phone-number/PhoneNumberGridComponent";
import UserProfileTimeslotsComponent from "./UserProfileTimeslotsComponent";
import UserProfilePasswordChangeFormComponent from "./UserProfilePasswordChangeFormComponent";
import { useApiService } from "../../services/ApiService";
import { LocalStorageService } from "../../services/LocalStorageService";
import { useTranslator } from "../../services/TranslatorService";
import { useParams } from "react-router-dom";
import { TabPanel, TabView } from "primereact/tabview";
import SubscriptionMemberTabComponent from "../subscription/SubscriptionMemberTabComponent";
import UserMedicalHistoryComponent from "./UserMedicalHistoryComponent";
import SubscriptionBalanceTag from "../subscription/SubscriptionBalanceTag";

export default function UserProfilePage() {
  const { t } = useTranslator();
  const params = useParams();
  const apiService = useApiService();

  const {
    userDto,
    updateUserDto,
    setUserDto,
    resetUserDto,
    userPasswordChangeDto,
  } = useUserStore();

  const [loading, setLoading] = useState(true);
  const [isImageHovered, setIsImageHovered] = useState(false);
  const [isImageUploadSelected, setIsImageUploadSelected] = useState(false);

  const userId: string | undefined = params["id"] ?? TokenService.getUserId();

  // A trainer opening somebody else's profile, rather than a member on their own.
  const isAdminView: boolean = params["id"] !== undefined;


  const [balance, setBalance] = useState<number | null>(null);

  // Change Password Dialog State
  const [showChangePasswordDialog, setShowChangePasswordDialog] =
    useState(false);

  // Compute display image src (prefix plain base64 if needed)
  const getDisplayImageSrc = (
    profileImage: string | undefined
  ): string | undefined => {
    if (!profileImage) return undefined;
    if (profileImage.startsWith("data:")) return profileImage;
    return `data:image/png;base64,${profileImage}`;
  };

  // Load initial data.
  useEffect(() => {
    resetUserDto();
    const loadUser = async () => {
      if (userId) {
        const response = await apiService.get<UserDto>("Users", userId);
        setUserDto(response as UserDto);
      }

      setLoading(false);
    };

    loadUser();
  }, []);

  // The badge by the name. The tab reads it too, but it is wanted before anybody
  // opens a tab, so it is fetched here as well rather than lifted out of the tab.
  useEffect(() => {
    const loadBalance = async () => {
      const response = await apiService.getSubscriptionBalance(
        isAdminView ? userId : undefined,
      );
      if (response !== null) setBalance(response);
    };

    loadBalance();
  }, [userId]);

  // Handle image upload
  const handleImageUpload = async (event: FileUploadHandlerEvent) => {
    const file = event.files[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onload = async () => {
        const fullDataUrl = reader.result as string;
        // Keep full data URL in DTO for display
        userDto.profileImage = fullDataUrl;

        // Extract plain base64 for API (split removes 'data:image/png;base64,' prefix)
        const plainBase64 = fullDataUrl.split(",")[1];

        // Create a copy for update with plain base64
        const updateDto: UserDto = { ...userDto, profileImage: plainBase64 };

        const response = await apiService.update<UserDto>(
          "Users",
          updateDto,
          userDto.id
        );
        if (response) {
          const id = params["id"];
          if (id === undefined) {
            LocalStorageService.setProfileImage(updateDto.profileImage ?? "");
          }

          if (
            response.profileImage &&
            !response.profileImage.startsWith("data:")
          ) {
            response.profileImage = `data:image/png;base64,${response.profileImage}`;
          }
          updateUserDto(response);
        }
      };
      reader.readAsDataURL(file);
    } catch (error) {
      console.error("Image upload failed:", error);
    }

    setIsImageUploadSelected(false);
  };

  // Handle Change Password Dialog
  const handleChangePassword = async () => {
    if (userId) {
      userPasswordChangeDto.userId = userId;
      const response = await apiService.passwordChange(userPasswordChangeDto);
      if (response) {
        setShowChangePasswordDialog(false);
      }
    }
  };

  const renderProfileImage = () => {
    if (userDto && userDto?.id) {
      const initials = `${userDto.firstName.charAt(0)}${userDto.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = getDisplayImageSrc(userDto.profileImage ?? "");
      return (
        <div
          style={{ position: "relative", display: "inline-block" }}
          onMouseEnter={() => setIsImageHovered(true)}
          onMouseLeave={() => setIsImageHovered(false)}
        >
          <Avatar
            image={imageSrc ?? ""}
            label={imageSrc ? undefined : initials}
            shape="circle"
            size="xlarge"
            style={{
              width: "80px",
              height: "80px",
              backgroundColor: imageSrc
                ? "transparent"
                : "var(--primary-color)",
              color: imageSrc ? "transparent" : "var(--surface-a)",
              fontSize: "2rem",
              transition: "opacity 0.3s",
              opacity: isImageHovered && imageSrc ? 0.7 : 1,
            }}
          />
          {isImageHovered && (
            <FileUpload
              mode="basic"
              auto
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
    <div className="grid">
      <div className="col-12">
        <Card
          className="p-4"
          style={{
            boxShadow: "0 4px 8px rgba(0, 0, 0, 0.1)",
          }}
        >
          <div className="flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
            <div className="flex align-items-center">
              <div className="mr-5">{renderProfileImage()}</div>
              <div>
                <h2 className="m-0">
                  {userDto?.firstName} {userDto?.lastName}
                </h2>
                <p className="text-color-secondary m-0">{userDto?.email}</p>
              </div>
            </div>

            <div className="flex align-items-center gap-3">
              <div className="flex align-items-center gap-2">
                <span className="text-color-secondary">
                  {t("Remaining lessons")}:
                </span>
                <SubscriptionBalanceTag
                  balance={balance ?? 0}
                  isAdminView={isAdminView}
                />
              </div>
              <Button
                label={t("Change Password")}
                icon="pi pi-key"
                className="p-button-outlined"
                onClick={() => setShowChangePasswordDialog(true)}
              />
            </div>
          </div>

          <TabView>
            <TabPanel
              header={t("Details")}
              leftIcon="pi pi-user mr-2"
            >
              <UserProfileFormComponent />
            </TabPanel>

            <TabPanel
              header={t("Phone Numbers")}
              leftIcon="pi pi-phone mr-2"
            >
              <PhoneNumberGridComponent />
            </TabPanel>

            <TabPanel
              header={t("Subscriptions")}
              leftIcon="pi pi-ticket mr-2"
            >
              {userId && (
                <SubscriptionMemberTabComponent
                  userId={userId}
                  isAdminView={isAdminView}
                />
              )}
            </TabPanel>

            <TabPanel
              header={t("Medical History")}
              leftIcon="pi pi-heart mr-2"
            >
              <UserMedicalHistoryComponent />
            </TabPanel>

            <TabPanel
              header={t("Next trainings")}
              leftIcon="pi pi-calendar mr-2"
            >
              <div style={{ minWidth: "700px" }}>
                <UserProfileTimeslotsComponent />
              </div>
            </TabPanel>
          </TabView>
        </Card>
      </div>

      {/* Change Password Dialog */}
      <Dialog
        header={t("Change Password")}
        visible={showChangePasswordDialog}
        onHide={() => setShowChangePasswordDialog(false)}
        style={{ width: "30rem" }}
        footer={
          <div className="flex justify-content-end gap-2">
            <Button
              label={t("Cancel")}
              icon="pi pi-times"
              className="p-button-text"
              onClick={() => setShowChangePasswordDialog(false)}
            />
            <Button
              label={t("Change Password")}
              icon="pi pi-check"
              className="p-button-primary"
              onClick={handleChangePassword}
              disabled={
                !userPasswordChangeDto.newPassword ||
                userPasswordChangeDto.newPassword !==
                  userPasswordChangeDto.confirmNewPassword
              }
            />
          </div>
        }
      >
        <UserProfilePasswordChangeFormComponent />
      </Dialog>
    </div>
  );
}
