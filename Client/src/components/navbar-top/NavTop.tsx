import { Menubar } from "primereact/menubar";
import { useNavigate } from "react-router-dom";
import { Menu } from "primereact/menu";
import { Button } from "primereact/button";
import { MenuItem } from "primereact/menuitem";
import { useRef, useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { TokenService } from "../../services/TokenService";
import { useApiService } from "../../services/ApiService";
import { Avatar } from "primereact/avatar";
import { LocalStorageService } from "../../services/LocalStorageService";
import { Chip } from "primereact/chip";
import { useTranslator } from "../../services/TranslatorService";
import NavSidebar from "../navbar-sidebar/NavSidebar";

export default function NavTop() {
  const { t } = useTranslator();
  const { isUserAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const menuRight = useRef<Menu>(null);
  const apiService = useApiService();
  const [navRightVisibility, setNavRightVisibility] = useState<boolean>(false);

  const items: MenuItem[] = [
    {
      label: "Home",
      icon: "pi pi-home",
      command: () => {
        navigate("/");
      },
    },
    {
      label: t("Apointment"),
      icon: "pi pi-book",
      command: () => {
        navigate("/appointment");
      },
    },
    {
      label: t("Workout Plans"),
      icon: "pi pi-clipboard",
      command: () => {
        navigate("/workout-plans");
      },
      visible: TokenService.isUserAllowed("WorkoutPlans_View"),
    },
    {
      label: "Admin",
      icon: "pi pi-building",
      visible:
        TokenService.isUserAllowed("Roles_View") ||
        TokenService.isUserAllowed("Users_View") ||
        TokenService.isUserAllowed("TrainGroups_View"),
      command: () => {
        navigate("/administrator");
      },
    },
    {
      label: t("Contact us"),
      icon: "pi pi-building-columns",
      command: () => {
        navigate("/contact-us");
      },
    },
    {
      label: t("Policy"),
      icon: "pi pi-building-columns",
      command: () => {
        navigate("/policy");
      },
    },
  ];

  const start = (
    <img
      alt="logo"
      src="https://primefaces.org/cdn/primereact/images/logo.png"
      height="40"
      className="mr-2"
    ></img>
  );

  const getItemsSettings = (): MenuItem[] => {
    let items: MenuItem[] = [
      {
        label: t("Options"),
        items: [
          {
            label: t("Settings"),
            icon: "pi pi-cog",
            command: () => setNavRightVisibility(true),
          },
        ],
      },
    ];

    if (isUserAuthenticated) {
      (items[0].items as MenuItem[])?.push({
        label: t("Profile"),
        icon: "pi pi-user",
        command: () => navigate("/users/profile"),
      });
      (items[0].items as MenuItem[])?.push({
        label: t("Logout"),
        icon: "pi pi-sign-out",
        command: () => {
          apiService.logout(logout);
          navigate("/users/login");
        },
      });
    } else {
      (items[0].items as MenuItem[])?.push({
        label: t("Login"),
        icon: "pi pi-user",
        command: () => {
          navigate("/users/login");
        },
      });

      (items[0].items as MenuItem[])?.push({
        label: t("Register"),
        icon: "pi pi-user",
        command: () => {
          navigate("/users/register");
        },
      });
    }

    return items;
  };

  const chipTemplate = () => {
    const firstName = LocalStorageService.getFirstName() ?? "";
    const lastName = LocalStorageService.getLastName() ?? "";
    const profileImage = LocalStorageService.getProfileImage();
    const isProfileImageSet = profileImage && profileImage?.length > 0;
    const imageSrc = `data:image/png;base64,${profileImage}`;
    if (firstName.length === 0 || lastName.length === 0) return "";

    const fullName =
      " " +
      firstName[0].toUpperCase() +
      ". " +
      lastName[0].toUpperCase() +
      lastName.slice(1, lastName.length);

    if (isProfileImageSet) {
      return (
        <Chip
          className=" transition-colors transition-duration-300 surface-section hover:bg-primary hover:text-gray-900 cursor-pointer "
          onClick={() => navigate("/users/profile")}
          template={
            <>
              <Avatar
                image={imageSrc}
                label={undefined}
                shape="circle"
                size="large"
                className=" mr-2 "
              />
              {fullName}
            </>
          }
        />
      );
    } else {
      return (
        <Chip
          className="transition-colors transition-duration-300 surface-section hover:bg-primary hover:text-gray-900 cursor-pointer "
          onClick={() => navigate("/users/profile")}
          label={fullName}
        />
      );
    }
  };

  const end = (
    <div className="flex align-items-center gap-2">
      <div className="pr-4">{chipTemplate()}</div>
      <Menu
        model={getItemsSettings()}
        popup
        ref={menuRight}
        id="popup_menu_right"
        popupAlignment="right"
      />
      <Button
        rounded
        outlined
        icon="pi pi-cog"
        aria-label="Filter"
        className="mr-2"
        onClick={(event) => menuRight.current?.toggle(event)}
        aria-controls="popup_menu_right"
        aria-haspopup
      />
    </div>
  );

  return (
    <>
      <div className="p-0 ">
        <Menubar
          model={items}
          start={start}
          end={end}
        />
      </div>

      <NavSidebar
        isVisible={navRightVisibility}
        hideSidebar={() => setNavRightVisibility(false)}
      />
    </>
  );
}
