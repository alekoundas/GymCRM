import { useRef } from "react";
import { ButtonTypeEnum } from "../../../enum/ButtonTypeEnum";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { TokenService } from "../../../services/TokenService";
import { Button } from "primereact/button";
import { useTranslator } from "../../../services/TranslatorService";
interface IField<TEntity> {
  rowData: TEntity;
  onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
  authorize: boolean;
  controller: string;
  availableGridRowButtons: ButtonTypeEnum[];
  // Per row: hide a button that makes no sense for this row, like Activate on a
  // row that is already active.
  isButtonVisible?: (buttonType: ButtonTypeEnum, rowData: TEntity) => boolean;
}

export default function DataTableGridRowActionsComponent<TEntity>({
  rowData,
  onButtonClick,
  authorize,
  controller,
  availableGridRowButtons,
  isButtonVisible,
}: IField<TEntity>) {
  const { t } = useTranslator();
  const menuRef = useRef<Menu>(null);

  const getMenuItems: () => MenuItem[] = () => {
    const menuItems: MenuItem[] = [];

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.PROFILE))
      menuItems.push({
        data: ButtonTypeEnum.PROFILE,
        label: t("Profile"),
        icon: "pi pi-user-edit",
        command: () => onButtonClick(ButtonTypeEnum.PROFILE, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_View")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.ATTENDANCES))
      menuItems.push({
        data: ButtonTypeEnum.ATTENDANCES,
        label: t("Attendances"),
        icon: "pi pi-address-book",
        command: () => onButtonClick(ButtonTypeEnum.ATTENDANCES, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_View")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.RECORDINGS))
      menuItems.push({
        data: ButtonTypeEnum.RECORDINGS,
        label: t("Recordings"),
        icon: "pi pi-history",
        command: () => onButtonClick(ButtonTypeEnum.RECORDINGS, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_View")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.VIEW))
      menuItems.push({
        data: ButtonTypeEnum.VIEW,
        label: t("View"),
        icon: "pi pi-eye",
        command: () => onButtonClick(ButtonTypeEnum.VIEW, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_View")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.EDIT))
      menuItems.push({
        data: ButtonTypeEnum.EDIT,
        label: t("Edit"),
        icon: "pi pi-pencil",
        command: () => onButtonClick(ButtonTypeEnum.EDIT, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Edit")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.CLONE))
      menuItems.push({
        data: ButtonTypeEnum.CLONE,
        label: t("Clone"),
        icon: "pi pi-copy",
        command: () => onButtonClick(ButtonTypeEnum.CLONE, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Add")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.DEACTIVATE))
      menuItems.push({
        data: ButtonTypeEnum.DEACTIVATE,
        label: t("Deactivate"),
        icon: "pi pi-eye-slash",
        command: () => onButtonClick(ButtonTypeEnum.DEACTIVATE, rowData),
        visible: authorize ? TokenService.isUserAllowed(controller + "_Edit") : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.ACTIVATE))
      menuItems.push({
        data: ButtonTypeEnum.ACTIVATE,
        label: t("Activate"),
        icon: "pi pi-eye",
        command: () => onButtonClick(ButtonTypeEnum.ACTIVATE, rowData),
        visible: authorize ? TokenService.isUserAllowed(controller + "_Edit") : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.DELETE))
      menuItems.push({
        data: ButtonTypeEnum.DELETE,
        label: t("Delete"),
        icon: "pi pi-trash",
        command: () => onButtonClick(ButtonTypeEnum.DELETE, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Delete")
          : true,
        style: { color: "red" }, // Optional: Red text for delete (Menu doesn't have built-in severity)
      });

    // Buttons the page says do not apply to this particular row.
    if (isButtonVisible)
      menuItems.forEach((item) => {
        if (!isButtonVisible(item.data as ButtonTypeEnum, rowData)) item.visible = false;
      });

    return menuItems;
  };

  return (
    <div>
      <Button
        icon="pi pi-ellipsis-v"
        rounded
        text
        onClick={(e) => menuRef.current?.toggle(e)}
      />
      <Menu
        ref={menuRef}
        closeOnEscape
        model={getMenuItems()}
        popup
        appendTo={document.body}
      />
    </div>
  );
}
