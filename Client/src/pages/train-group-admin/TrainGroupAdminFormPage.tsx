import { Card } from "primereact/card";
import { useEffect, useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import TrainGroupDateGridComponent from "../train-group-date/TrainGroupDateGridComponent";
import TrainGroupParticipantGridComponent from "../train-group-participant/TrainGroupParticipantGridComponent";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "primereact/button";
import TrainGroupDateParticipantGridComponent from "../train-group-participant/TrainGroupDateParticipantGridComponent";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupAdminFormPage({ formMode }: IField) {
  const params = useParams();
  const navigate = useNavigate();

  const { trainGroupDto, resetTrainGroupDto, resetSelectedTrainGroupDate } =
    useTrainGroupStore();

  // Load Initial data
  useEffect(() => {
    if (params["id"]) {
      const id = params["id"];
      resetTrainGroupDto(+id);
    }
  }, []);

  const onSave = async () => {
    // Reset negative ids
    // trainGroupDto.trainGroupDates = trainGroupDto.trainGroupDates.map((x) => ({
    //   ...x,
    //   id: 0,
    // }));
    // trainGroupDto.trainGroupParticipants =
    //   trainGroupDto.trainGroupParticipants.map((x) => ({ ...x, id: 0 }));

    if (formMode === FormMode.ADD) {
      const response = await ApiService.create("trainGroups", trainGroupDto);
      if (response?.[0]) {
        navigate(
          "/administrator/train-groups/" + response[0]?.["id"] + "/view"
        );
      }
    } else {
      const response = await ApiService.update(
        "trainGroups",
        trainGroupDto,
        trainGroupDto.id
      );
    }
  };

  return (
    <>
      <div className="grid ">
        <div className=" col-12  lg:col-6 xl:col-6">
          <Card
            title="Train Group"
            footer={
              <div className="flex justify-content-between">
                <div></div>
                <Button
                  label="Save"
                  icon="pi pi-check"
                  onClick={onSave}
                  visible={formMode !== FormMode.VIEW}
                  autoFocus
                />
              </div>
            }
          >
            <div className="card">
              <TrainGroupFormComponent formMode={formMode} />
            </div>
          </Card>
        </div>
        <div className=" col-12  lg:col-6 xl:col-6">
          <Card title="Train Group Dates">
            <div className="card">
              <TrainGroupDateGridComponent formMode={formMode} />
            </div>
          </Card>
        </div>

        <div className=" col-12  lg:col-6 xl:col-6">
          <Card title="Train Group Participants">
            <div className="card">
              <TrainGroupParticipantGridComponent formMode={formMode} />
            </div>
          </Card>
        </div>
        <div className=" col-12  lg:col-6 xl:col-6">
          <Card title="Train Group Date Participants">
            <div className="card">
              <TrainGroupDateParticipantGridComponent formMode={formMode} />
            </div>
          </Card>
        </div>
      </div>

      {/*                                     */}
      {/*           Add Train Group           */}
      {/*                                     */}

      {/* <GenericDialogComponent
        visible={isAddDialogVisible}
        control={dialogControlAdd}
        onSave={OnSaveAdd}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.ADD} />
          <TrainGroupDateGridComponent formMode={FormMode.ADD} />
        </div>
      </GenericDialogComponent> */}

      {/*                                     */}
      {/*          Edit Train Group           */}
      {/*                                     */}
      {/* <GenericDialogComponent
        visible={isEditDialogVisible}
        control={dialogControlEdit}
        onSave={OnSaveEdit}
      >
        <div className="w-full">
          <TrainGroupFormComponent formMode={FormMode.EDIT} />
          <TrainGroupDateGridComponent formMode={FormMode.EDIT} />
        </div>
      </GenericDialogComponent> */}
    </>
  );
}
