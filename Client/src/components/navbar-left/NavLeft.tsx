import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { useNavigate } from "react-router-dom";
import { useTranslator } from "../../services/TranslatorService";
import { TokenService } from "../../services/TokenService";
import { Button } from "primereact/button";
import { useState } from "react";
import { Card } from "primereact/card";

export default function NavLeft() {
  const { t } = useTranslator();
  const navigate = useNavigate();
  const [expanded, setExpanded] = useState(true); // start expanded or false if you prefer collapsed by default

  const baseItems: MenuItem[] = [
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
          command: () => navigate("/administrator/roles"),
        },
        {
          label: t("Users"),
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("Users_View"),
          command: () => navigate("/administrator/users"),
        },
        {
          label: t("User Statuses"),
          icon: "pi pi-palette",
          visible: TokenService.isUserAllowed("UserStatuses_View"),
          command: () => navigate("/administrator/user-statuses"),
        },
      ],
    },
    // ... other menu groups (Trainer, Email) same as before
    {
      label: t("Trainer"),
      visible: TokenService.isUserAllowed("TrainGroups_View"),
      items: [
        {
          label: t("Calendar"),
          icon: "pi pi-calendar",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => navigate("/administrator/train-group-calendar"),
        },
        {
          label: t("Train Groups"),
          icon: "pi pi-users",
          visible: TokenService.isUserAllowed("TrainGroups_View"),
          command: () => navigate("/administrator/train-groups"),
        },
        {
          label: t("Workout Plans"),
          icon: "pi pi-clipboard",
          visible: TokenService.isUserAllowed("WorkoutPlans_View"),
          command: () => navigate("/administrator/workout-plans"),
        },
      ],
    },
    {
      label: "Email",
      visible: TokenService.isUserAllowed("Mails_View"),
      items: [
        {
          label: "Google",
          icon: "pi pi-google",
          visible: TokenService.isUserAllowed("Roles_Edit"),
          command: () => navigate("/administrator/google"),
        },
        {
          label: "Emails",
          icon: "pi pi-envelope",
          visible: TokenService.isUserAllowed("Mails_View"),
          command: () => navigate("/administrator/emails"),
        },
        {
          label: t("Send new Mail"),
          icon: "pi pi-send",
          visible: TokenService.isUserAllowed("Mails_Add"),
          command: () => navigate("/administrator/email-send"),
        },
      ],
    },
  ];

  // Transform items: hide labels when collapsed
  const menuItems = expanded
    ? baseItems
    : baseItems.map((group) => ({
        ...group,
        label: undefined, // hide group label
        items: group.items?.map((item: MenuItem) => ({
          ...item,
          label: undefined, // hide item labels
          tooltip: item.label, // optional: show on hover
          tooltipOptions: { position: "right" },
        })),
      }));

  return (
    <Card className="h-full ">
      {/* Toggle Button at the top */}
      <div className="p-3 border-bottom-2 border-top-2 surface-border">
        <Button
          icon={expanded ? "pi pi-chevron-left" : "pi pi-chevron-right"}
          onClick={() => setExpanded(!expanded)}
          className="p-button-text  w-full"
          // tooltip={expanded ? "Collapse menu" : "Expand menu"}
          // tooltipOptions={{ position: "right" }}
        />
      </div>

      {/* Menu */}
      <div className="flex-1 overflow-y-auto">
        <Menu
          model={menuItems}
          className={expanded ? "w-full" : "w-4rem"} // 64px when collapsed
          style={{ border: "none", background: "transparent" }}
        />
      </div>
    </Card>
  );
}
