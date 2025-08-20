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
      label: "",
      items: [
        {
          label: "Customers",
          icon: "pi pi-user",
          command: () => {
            navigate("/administrator/customers");
          },
        },
        // {
        //   label: "Tickets",
        //   icon: "pi pi-ticket",
        //   command: () => {
        //     navigate("/administrator/tickets");
        //   },
        // },
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
      label: "Lookups",
      items: [
        // {
        //   label: "Makers",
        //   icon: "pi pi-database",
        //   command: () => {
        //     navigate("/administrator/makers");
        //   },
        // },
        // {
        //   label: "Maker Models",
        //   icon: "pi pi-database",
        //   command: () => {
        //     navigate("/administrator/makermodels");
        //   },
        // },
      ],
    },
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
