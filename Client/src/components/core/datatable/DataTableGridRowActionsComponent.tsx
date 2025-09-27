import { useRef } from "react";
import { ButtonTypeEnum } from "../../../enum/ButtonTypeEnum";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { TokenService } from "../../../services/TokenService";
import { Button } from "primereact/button";
interface IField<TEntity> {
  rowData: TEntity;
  onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
  authorize: boolean;
  controller: string;
  availableGridRowButtons: ButtonTypeEnum[];
}

export default function DataTableGridRowActionsComponent<TEntity>({
  rowData,
  onButtonClick,
  authorize,
  controller,
  availableGridRowButtons,
}: IField<TEntity>) {
  const menuRef = useRef<Menu>(null);

  const getMenuItems: () => MenuItem[] = () => {
    const menuItems: MenuItem[] = [];

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.VIEW))
      menuItems.push({
        label: "View",
        icon: "pi pi-eye",
        command: () => onButtonClick(ButtonTypeEnum.VIEW, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_View")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.EDIT))
      menuItems.push({
        label: "Edit",
        icon: "pi pi-pencil",
        command: () => onButtonClick(ButtonTypeEnum.EDIT, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Edit")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.CLONE))
      menuItems.push({
        label: "Clone",
        icon: "pi pi-copy",
        command: () => onButtonClick(ButtonTypeEnum.CLONE, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Add")
          : true,
      });

    if (availableGridRowButtons.some((x) => x === ButtonTypeEnum.DELETE))
      menuItems.push({
        label: "Delete",
        icon: "pi pi-trash",
        command: () => onButtonClick(ButtonTypeEnum.DELETE, rowData),
        visible: authorize
          ? TokenService.isUserAllowed(controller + "_Delete")
          : true,
        style: { color: "red" }, // Optional: Red text for delete (Menu doesn't have built-in severity)
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
