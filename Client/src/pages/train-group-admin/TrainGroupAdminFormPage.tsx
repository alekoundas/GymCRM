import { Card } from "primereact/card";
import { useEffect, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import TrainGroupFormComponent from "./TrainGroupFormComponent";
import TrainGroupDateGridComponent from "../train-group-date/TrainGroupDateGridComponent";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "primereact/button";
import TrainGroupDateParticipantGridComponent from "../train-group-participant/TrainGroupDateParticipantGridComponent";
import TrainGroupDateOneOffParticipantGridComponent from "../train-group-participant/TrainGroupDateOneOffParticipantGridComponent";
import { Dialog } from "primereact/dialog";
import { TrainGroupDateDto } from "../../model/entities/train-group-date/TrainGroupDateDto";
import { TrainGroupDto } from "../../model/entities/train-group/TrainGroupDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  formMode: FormMode;
}

export default function TrainGroupAdminFormPage({ formMode }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const params = useParams();
  const navigate = useNavigate();

  const { trainGroupDto, updateTrainGroupDto } = useTrainGroupStore();
  const [isInfoDateDialogVisible, setInfoDateDialogVisible] = useState(false); // Dialog visibility
  const [isInfoParticipantDialogVisible, setInfoParticipantDialogVisible] =
    useState(false); // Dialog visibility

  // Load Initial data
  useEffect(() => {
    if (params["id"]) {
      const id = params["id"];
      apiService.get<TrainGroupDto>("TrainGroups", id).then((response) => {
        if (response) {
          updateTrainGroupDto({ id: response.id });
          updateTrainGroupDto({ title: response.title });
          updateTrainGroupDto({ startOn: response.startOn });
          updateTrainGroupDto({ duration: response.duration });
          updateTrainGroupDto({ maxParticipants: response.maxParticipants });
          updateTrainGroupDto({ trainer: response.trainer });
          updateTrainGroupDto({ trainerId: response.trainerId });
          updateTrainGroupDto({ description: response.description });
        }
      });
    }
  }, []);

  const onSave = async () => {
    if (formMode === FormMode.ADD) {
      const cleanedTrainGroupDates: TrainGroupDateDto[] =
        trainGroupDto.trainGroupDates.map((x: TrainGroupDateDto) => ({
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

      const response = await apiService.create(
        "trainGroups",
        createTrainGroupDto
      );
      if (response?.[0]) {
        // navigate(
        //   "/administrator/train-groups/" + response[0]?.["id"] + "/view"
        // );
        navigate("/administrator/train-groups");
      }
    } else {
      const response = await apiService.update(
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
            title={t("Train Group")}
            footer={
              <div className="flex justify-content-between">
                <div></div>
                <Button
                  label={t("Save")}
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
                  <h2 className="m-0">{t("Train Group Dates")}</h2>
                  <p className="m-0 text-gray-600">
                    {t("Select a date to view Participants")}
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
                    {t("Train Group Date Participants (One Off)")}
                  </h2>
                  <p className="m-0 text-gray-600">
                    {t("One-off participants")}
                  </p>
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
                  <h2 className="m-0">{t("Train Group Date Participants")}</h2>
                  <p className="m-0 text-gray-600">
                    {t("Recurring participants")}
                  </p>
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
        <p>{t("Handle the date or dates when this Training Group occurs")}.</p>
        <p>{t("Training Group Dates must follow these rules")}:</p>
        <ul>
          <li>{t("Cannot mix day-of-week dates with day-of-month dates")}.</li>
          <li>
            {t("Fixed dates cannot overlap with an existing day-of-week row")}.
          </li>
          <li>
            {t("Fixed dates cannot overlap with an existing day-of-month row")}.
          </li>
          <li>{t("Duplicate dates are not allowed")}.</li>
        </ul>
      </Dialog>

      <Dialog
        header={t("Train group participants")}
        visible={isInfoParticipantDialogVisible}
        style={{ width: "50vw" }}
        onHide={() => {
          if (!isInfoParticipantDialogVisible) return;
          setInfoParticipantDialogVisible(false);
        }}
      >
        <p>
          {t(
            "Handle the participants of the selected train group date. If Selected Date is set, then this is an one-off participant and will only join the specific train group date once."
          )}
        </p>
        <p>{t("Training Group Date Participants must follow these rules")}:</p>
        <ul>
          <li>
            {t(
              "If selected date is set (one-off), then it should match at least one train group date"
            )}
            .
          </li>

          <li>
            {t(
              "Selected Date cannot be set to a Fixed Date. Fixed date is already a single date and cant have one-off participants"
            )}
            .
          </li>
          <li>{t("Duplicate participants are not allowed")}.</li>
        </ul>
      </Dialog>
    </>
  );
}
