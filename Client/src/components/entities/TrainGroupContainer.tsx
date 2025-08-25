import { Card } from "primereact/card";
import React from "react";
import { Button } from "primereact/button";
import { useNavigate } from "react-router-dom";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupDateGrid from "./train-group-date/TrainGroupDateGrid";
import TrainGroupForm from "./train-group/TrainGroupForm";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupContainer({ formMode }: IField) {
  const { resetTrainGroupDto, trainGroupDto } = useTrainGroupStore();
  const navigate = useNavigate();

  const handleSave = async () => {
    const response = await ApiService.create("trainGroup", trainGroupDto);

    if (response) {
      resetTrainGroupDto();
      navigate("/administrator/train-group");
    }
  };

  const handleCancel = async () => {
    resetTrainGroupDto;
    navigate("/administrator/train-group");
  };

  const footer = () => (
    <React.Fragment>
      <Button
        label="Cancel"
        icon="pi pi-times"
        outlined
        onClick={handleCancel}
      />
      <Button
        label="Save"
        icon="pi pi-check"
        onClick={handleSave}
      />
    </React.Fragment>
  );

  return (
    <>
      <div className="flex flex-row flex-wrap align-items-center justify-content-center">
        <Card
          title="Train Group Form"
          footer={footer}
        >
          <TrainGroupForm formMode={formMode} />
          <TrainGroupDateGrid formMode={formMode} />
        </Card>
      </div>
    </>
  );
}
