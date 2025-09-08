import { Card } from "primereact/card";
import { useEffect } from "react";
import { FormMode } from "../../enum/FormMode";
import ApiService from "../../services/ApiService";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import TrainGroupDateGridComponent from "../train-group-date/TrainGroupDateGridComponent";
import TrainGroupParticipantGridComponent from "../train-group-participant/TrainGroupParticipantGridComponent";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "primereact/button";
import TrainGroupDateParticipantGridComponent from "../train-group-participant/TrainGroupDateParticipantGridComponent";
import { TrainGroupDateDto } from "../../model/TrainGroupDateDto";
import { TrainGroupDto } from "../../model/TrainGroupDto";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupAdminFormPage({ formMode }: IField) {
  const params = useParams();
  const navigate = useNavigate();

  const { trainGroupDto, resetTrainGroupDto } = useTrainGroupStore();

  // Load Initial data
  useEffect(() => {
    if (params["id"]) {
      const id = params["id"];
      resetTrainGroupDto(+id);
    }
  }, []);

  const onSave = async () => {
    if (formMode === FormMode.ADD) {
      const cleanedTrainGroupDates: TrainGroupDateDto[] =
        trainGroupDto.trainGroupDates.map((x) => ({
          ...x,
          trainGroupId: undefined,
          trainGroupParticipants: x.trainGroupParticipants.map((y) => ({
            ...y,
            trainGroupDateId: undefined,
            trainGroupId: 0,
          })),
        }));
      const createTrainGroupDto: TrainGroupDto = {
        ...trainGroupDto,
        trainGroupDates: cleanedTrainGroupDates,
      };

      const response = await ApiService.create(
        "trainGroups",
        createTrainGroupDto
      );
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
    </>
  );
}
