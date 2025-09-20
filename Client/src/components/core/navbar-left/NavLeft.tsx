import { useRef } from "react";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { Toast } from "primereact/toast";
import { useNavigate } from "react-router-dom";
import { TokenService } from "../../../services/TokenService";

export default function NavLeft() {
  const toast = useRef<Toast>(null);
  const navigate = useNavigate();

  const items: MenuItem[] = [
    {
      label: "Admin",
      visible:
        TokenService.isUserAllowed("Users_View") ||
        TokenService.isUserAllowed("Roles_View"),
      items: [
        {
          label: "Users",
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("Users_View"),
          command: () => {
            navigate("/administrator/users");
          },
        },
        {
          label: "Roles",
          icon: "pi pi-key",
          visible: TokenService.isUserAllowed("Roles_View"),
          command: () => {
            navigate("/administrator/roles");
          },
        },
      ],
    },
    {
      label: "Trainer",
      visible: TokenService.isUserAllowed("TrainGroups_View"),
      items: [
        {
          label: "Calendar",
          icon: "pi pi-calendar",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => {
            navigate("/administrator/train-group-calendar");
          },
        },
        {
          label: "Train Groups",
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => {
            navigate("/administrator/train-groups");
          },
        },
      ],
    },

    {
      label: "Email",
      visible: TokenService.isUserAllowed("Mails_View"),
      items: [
        {
          label: "Emails",
          icon: "pi pi-envelope",
          visible: TokenService.isUserAllowed("Mails_View"),
          command: () => {
            navigate("/administrator/emails");
          },
        },
      ],
    },
    // {
    //   label: "Lookups",
    //   items: [
    //     // {
    //     //   label: "Makers",
    //     //   icon: "pi pi-database",
    //     //   command: () => {
    //     //     navigate("/administrator/makers");
    //     //   },
    //     // },
    //     // {
    //     //   label: "Maker Models",
    //     //   icon: "pi pi-database",
    //     //   command: () => {
    //     //     navigate("/administrator/makermodels");
    //     //   },
    //     // },
    //   ],
    // },
  ];

  return (
    <>
      <div
        className="card "
        style={{ height: "93vh" }}
      >
        <Toast ref={toast} />
        <Menu
          model={items}
          className="h-full w-full"
          // className="h-full w-full lg:w-15rem"
        />
      </div>
    </>
  );
}
