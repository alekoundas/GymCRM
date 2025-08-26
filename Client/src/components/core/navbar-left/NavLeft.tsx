import { useRef } from "react";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { Toast } from "primereact/toast";
import { useNavigate } from "react-router-dom";

export default function NavLeft() {
  const toast = useRef<Toast>(null);
  const navigate = useNavigate();

  const items: MenuItem[] = [
    {
      label: "Admin",
      items: [
        {
          label: "Users",
          icon: "pi pi-users",
          command: () => {
            navigate("/administrator/users");
          },
        },
        {
          label: "Roles",
          icon: "pi pi-key",
          command: () => {
            navigate("/administrator/roles");
          },
        },
      ],
    },
    {
      label: "Trainer",
      items: [
        {
          label: "Calendar",
          icon: "pi pi-calendar",
          command: () => {
            navigate("/administrator/train-group-calendar");
          },
        },
        {
          label: "Train Groups",
          icon: "pi pi-users",
          command: () => {
            navigate("/administrator/train-groups");
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
