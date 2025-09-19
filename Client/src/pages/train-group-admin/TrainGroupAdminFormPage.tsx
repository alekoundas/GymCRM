import { Card } from "primereact/card";
import { useEffect, useState } from "react";
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
import TrainGroupDateOneOffParticipantGridComponent from "../train-group-participant/TrainGroupDateOneOffParticipantGridComponent";
import { Dialog } from "primereact/dialog";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupAdminFormPage({ formMode }: IField) {
  const params = useParams();
  const navigate = useNavigate();

  const { trainGroupDto, resetTrainGroupDto } = useTrainGroupStore();
  const [isInfoDateDialogVisible, setInfoDateDialogVisible] = useState(false); // Dialog visibility
  const [isInfoParticipantDialogVisible, setInfoParticipantDialogVisible] =
    useState(false); // Dialog visibility

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
          <Card
            header={
              <div className="flex justify-content-between align-items-center p-3">
                <div className="flex flex-column gap-1">
                  <h2 className="m-0">Train Group Dates</h2>
                  <p className="m-0 text-gray-600">
                    Select a date to view Participants
                  </p>
                </div>
                <Button
                  label=""
                  icon="pi pi-info-circle"
                  onClick={() => setInfoDateDialogVisible(true)}
                  className="p-button-text"
                />
              </div>
            }
          >
            <div className="card">
              <TrainGroupDateGridComponent formMode={formMode} />
            </div>
          </Card>
        </div>

        <div className=" col-12  lg:col-6 xl:col-6">
          <Card
            header={
              <div className="flex justify-content-between align-items-center p-3">
                <div className="flex flex-column gap-1">
                  <h2 className="m-0">
                    Train Group Date Participants (One Off)
                  </h2>
                  <p className="m-0 text-gray-600">One-off participants</p>
                </div>
                <Button
                  label=""
                  icon="pi pi-info-circle"
                  onClick={() => setInfoParticipantDialogVisible(true)}
                  className="p-button-text"
                />
              </div>
            }
          >
            <div className="card">
              <TrainGroupDateOneOffParticipantGridComponent
                formMode={formMode}
              />
            </div>
          </Card>
        </div>
        <div className=" col-12  lg:col-6 xl:col-6">
          <Card
            header={
              <div className="flex justify-content-between align-items-center p-3">
                <div className="flex flex-column gap-1">
                  <h2 className="m-0">Train Group Date Participants </h2>
                  <p className="m-0 text-gray-600">Recurring participants</p>
                </div>
                <Button
                  label=""
                  icon="pi pi-info-circle"
                  onClick={() => setInfoParticipantDialogVisible(true)}
                  className="p-button-text"
                />
              </div>
            }
          >
            <div className="card">
              <TrainGroupDateParticipantGridComponent formMode={formMode} />
            </div>
          </Card>
        </div>
      </div>

      {/*                                       */}
      {/*             Info Dialog               */}
      {/*                                       */}
      <Dialog
        header="Train group dates"
        visible={isInfoDateDialogVisible}
        style={{ width: "50vw" }}
        onHide={() => {
          if (!isInfoDateDialogVisible) return;
          setInfoDateDialogVisible(false);
        }}
      >
        <p>Handle the date or dates when this Training Group occurs.</p>
        <p>Training Group Dates must follow these rules:</p>
        <ul>
          <li>Cannot mix day-of-week dates with day-of-month dates.</li>
          <li>Fixed dates cannot overlap with an existing day-of-week row.</li>
          <li>Fixed dates cannot overlap with an existing day-of-month row.</li>
          <li>Duplicate dates are not allowed.</li>
        </ul>
      </Dialog>

      <Dialog
        header="Train group participants"
        visible={isInfoParticipantDialogVisible}
        style={{ width: "50vw" }}
        onHide={() => {
          if (!isInfoParticipantDialogVisible) return;
          setInfoParticipantDialogVisible(false);
        }}
      >
        <p>
          Handle the participants of the selected train group date. If "Selected
          Date" is set, then this is an one-off participant and will only join
          the specific train group date once.
        </p>
        <p>Training Group Date Participants must follow these rules:</p>
        <ul>
          <li>
            If selected date is set (one-off), then it should match at least one
            train group date.
          </li>

          <li>
            "Selected Date" cannot be set to a Fixed Date. Fixed date is already
            a single date and cant have one-off participants.
          </li>
          <li>Duplicate participants are not allowed.</li>
        </ul>
      </Dialog>
    </>
  );
}
