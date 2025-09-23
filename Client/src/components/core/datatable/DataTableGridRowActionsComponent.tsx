import { useEffect, useRef } from "react";
import { ButtonTypeEnum } from "../../../enum/ButtonTypeEnum";
import { Menu } from "primereact/menu";
import { MenuItem } from "primereact/menuitem";
import { TokenService } from "../../../services/TokenService";
import { Button } from "primereact/button";
import { useClickOutside } from "primereact/hooks";
interface IField<TEntity> {
  rowData: TEntity;
  onButtonClick: (type: ButtonTypeEnum, data: TEntity) => void;
  authorize: boolean;
  controller: string;
}

export default function DataTableGridRowActionsComponent<TEntity>({
  rowData,
  onButtonClick,
  authorize,
  controller,
}: IField<TEntity>) {
  const menuRef = useRef<Menu>(null);

  // useEffect(() => {
  //   const handleClickOutside = (event: MouseEvent) => {
  //     const menuTarget = menuRef.current?.getElement();
  //     if (menuTarget) {
  //       let isMenuClick = false;

  //       menuTarget?.childNodes.forEach((ul) => {
  //         ul.childNodes.forEach((li) => {
  //           li.childNodes.forEach((div) => {
  //             div.childNodes.forEach((a) => {
  //               if (a === event.target) isMenuClick = true;
  //             });
  //           });
  //         });
  //       });

  //       if (!isMenuClick) menuTarget.hidden = true;
  //     }
  //   };

  //   // Attach listener after a short delay to avoid initial open interference
  //   const timeoutId = setTimeout(() => {
  //     document.addEventListener("mousedown", handleClickOutside);
  //   }, 0);

  //   return () => {
  //     clearTimeout(timeoutId);
  //     document.removeEventListener("mousedown", handleClickOutside);
  //   };
  // }, []); // Runs once per component instance

  const actionMenuItems: MenuItem[] = [
    {
      label: "View",
      icon: "pi pi-eye",
      command: () => onButtonClick(ButtonTypeEnum.VIEW, rowData),
      visible: authorize
        ? TokenService.isUserAllowed(controller + "_View")
        : true,
    },
    {
      label: "Edit",
      icon: "pi pi-pencil",
      command: () => onButtonClick(ButtonTypeEnum.EDIT, rowData),
      visible: authorize
        ? TokenService.isUserAllowed(controller + "_Edit")
        : true,
    },
    {
      label: "Delete",
      icon: "pi pi-trash",
      command: () => onButtonClick(ButtonTypeEnum.DELETE, rowData),
      visible: authorize
        ? TokenService.isUserAllowed(controller + "_Delete")
        : true,
      style: { color: "red" }, // Optional: Red text for delete (Menu doesn't have built-in severity)
    },
  ];

  // const handleToggle = (e: React.MouseEvent) => {
  //   menuRef.current?.toggle(e);
  // };

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
        model={actionMenuItems}
        popup
        appendTo={document.body}
      />
    </div>
  );
}
