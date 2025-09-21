import { useRef, useState } from "react";
import { Card } from "primereact/card";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import ApiService from "../../services/ApiService";

import { useMailStore } from "../../stores/MailStore";
import { MailDto } from "../../model/entities/mail/MailDto";
import MailSendFormComponent from "./MailSendFormComponent";

export default function MailSendPage() {
  const { mailDto, setMailDto, resetMailDto, updateMailDto } = useMailStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<MailDto>) => void) | undefined
  >(undefined);

  const [isViewDialogVisible, setViewDialogVisibility] = useState(false); // Dialog visibility

  const OnSaveAdd = async () => {
    const response = await ApiService.create("mails", MailDto);

    if (response) {
      // dialogControlAdd.hideDialog();
      resetMailDto();
    }
  };

  return (
    <>
      <Card title="Email History">
        <MailSendFormComponent />
      </Card>
    </>
  );
}
