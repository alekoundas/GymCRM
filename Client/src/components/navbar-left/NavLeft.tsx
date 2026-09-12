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
  // Expanded the menu is about 370px wide, which on a phone is the whole screen and
  // leaves the page beside it nothing at all. There it starts as an icon rail instead;
  // the toggle still opens it either way.
  const [expanded, setExpanded] = useState(() => window.innerWidth >= 768);

  // A group is shown when at least one thing inside it is. Working that out from the
  // children rather than restating the claims on the group is what stops a screen
  // becoming unreachable: the old Trainer group named neither the recordings nor the
  // subscription claims, so somebody holding only those saw no group at all.
  const group = (label: string, items: MenuItem[]): MenuItem => ({
    label,
    visible: items.some((item) => item.visible),
    className: "mt-3",
    items,
  });

  const baseItems: MenuItem[] = [
    // What the gym does day to day. Ordered by how often it is opened, with the
    // configuration screen last.
    group(t("Training"), [
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
        visible: TokenService.isUserAllowed("WorkoutPlansAdmin_View"),
        command: () => navigate("/administrator/workout-plans"),
      },
      {
        label: t("Recordings"),
        icon: "pi pi-history",
        visible: TokenService.isUserAllowed("WorkoutPlanRecordingsAdmin_View"),
        command: () => navigate("/administrator/workout-plan-recordings"),
      },
      {
        label: t("Workout plan rules"),
        icon: "pi pi-sliders-h",
        visible: TokenService.isUserAllowed("WorkoutPlanRules_View"),
        command: () => navigate("/administrator/workout-plan-rules"),
      },
    ]),

    // Their own heading rather than buried under the trainer - this is the membership
    // side of the gym, and the requests are an inbox somebody has to work through, so
    // they come before the ledger.
    group(t("Subscriptions"), [
      {
        label: t("Subscription requests"),
        icon: "pi pi-inbox",
        visible: TokenService.isUserAllowed("SubscriptionsAdmin_Edit"),
        command: () => navigate("/administrator/subscription-requests"),
      },
      {
        label: t("Subscriptions"),
        icon: "pi pi-ticket",
        visible: TokenService.isUserAllowed("SubscriptionsAdmin_View"),
        command: () => navigate("/administrator/subscriptions"),
      },
    ]),

    // Set up once and rarely touched afterwards, so it sits below the daily work.
    group(t("Administration"), [
      {
        label: t("Users"),
        // A single figure, because the collapsed menu is icons only and the group
        // icon next door already means "several people".
        icon: "pi pi-user",
        visible: TokenService.isUserAllowed("Users_View"),
        command: () => navigate("/administrator/users"),
      },
      {
        label: t("User Statuses"),
        icon: "pi pi-palette",
        visible: TokenService.isUserAllowed("UserStatuses_View"),
        command: () => navigate("/administrator/user-statuses"),
      },
      {
        label: t("Roles"),
        icon: "pi pi-key",
        visible: TokenService.isUserAllowed("Roles_View"),
        command: () => navigate("/administrator/roles"),
      },
    ]),

    group("Email", [
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
    ]),
  ];

  // Transform items: hide labels when collapsed
  const menuItems = expanded
    ? baseItems
    : baseItems.map((menuGroup) => ({
        ...menuGroup,
        label: undefined, // hide group label
        items: menuGroup.items?.map((item: MenuItem) => ({
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
