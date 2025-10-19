import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { useNavigate } from "react-router-dom";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";

export default function NavLeft() {
  const { t } = useTranslator();
  const navigate = useNavigate();

  const items: MenuItem[] = [
    {
      label: "Admin",
      visible:
        TokenService.isUserAllowed("Users_View") ||
        TokenService.isUserAllowed("Roles_View"),
      items: [
        {
          label: t("Roles"),
          icon: "pi pi-key",
          visible: TokenService.isUserAllowed("Roles_View"),
          command: () => {
            navigate("/administrator/roles");
          },
        },
        {
          label: t("Users"),
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("Users_View"),
          command: () => {
            navigate("/administrator/users");
          },
        },
        {
          label: t("User Statuses"),
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("UserStatuses_View"),
          command: () => {
            navigate("/administrator/user-statuses");
          },
        },
      ],
    },
    {
      label: t("Trainer"),
      visible: TokenService.isUserAllowed("TrainGroups_View"),
      items: [
        {
          label: t("Calendar"),
          icon: "pi pi-calendar",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => {
            navigate("/administrator/train-group-calendar");
          },
        },
        {
          label: t("Train Groups"),
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => {
            navigate("/administrator/train-groups");
          },
        },

        {
          label: t("Workout Plans"),
          icon: "pi pi-clipboard",
          visible: TokenService.isUserAllowed("WorkoutPlans_View"),
          command: () => {
            navigate("/administrator/workout-plans");
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
        {
          label: t("Send new Mail"),
          icon: "pi pi-send",
          visible: TokenService.isUserAllowed("Mails_Add"),
          command: () => {
            navigate("/administrator/email-send");
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
        className="card h-full"
        // style={{ height: "100%" }}
      >
        {/* <Toast ref={toast} /> */}
        <Menu
          model={items}
          className="h-full w-full"
          // className="h-full w-full lg:w-15rem"
        />
      </div>
    </>
  );
}
