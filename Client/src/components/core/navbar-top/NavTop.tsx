import { Menubar } from "primereact/menubar";
import { Badge } from "primereact/badge";
import { useNavigate } from "react-router-dom";
import { Menu } from "primereact/menu";
import { Button } from "primereact/button";
import { MenuItem } from "primereact/menuitem";
import { useRef, useState } from "react";
import { useAuth } from "../../../contexts/AuthContext";
import { TokenService } from "../../../services/TokenService";
import { useApiService } from "../../../services/ApiService";
import NavSidebar from "../navbar-sidebar/NavSidebar";

export default function NavTop() {
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
      label: "Apointment",
      icon: "pi pi-book",
      command: () => {
        navigate("/appointment");
      },
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
        label: "Options",
        items: [
          {
            label: "Theme",
            icon: "pi pi-palette",
            command: () => setNavRightVisibility(true),
          },

          // {
          //   separator: true,
          // },
          // {
          //   template: dialogFooter,
          // },
        ],
      },
    ];

    if (isUserAuthenticated) {
      (items[0].items as MenuItem[])?.push({
        label: "Logout",
        icon: "pi pi-user",
        command: () => {
          apiService.logout(logout);
          navigate("/users/login");
        },
      });
      (items[0].items as MenuItem[])?.push({
        label: "Profile",
        icon: "pi pi-user",
        command: () => navigate("/users/profile"),
      });
    } else {
      (items[0].items as MenuItem[])?.push({
        label: "Login",
        icon: "pi pi-user",
        command: () => {
          navigate("/users/login");
        },
      });

      (items[0].items as MenuItem[])?.push({
        label: "Register",
        icon: "pi pi-user",
        command: () => {
          navigate("/users/register");
        },
      });
    }

    return items;
  };

  const end = (
    <div>
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
