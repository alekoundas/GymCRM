import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { InputText } from "primereact/inputtext";
import GenericDialogComponent from "../../components/core/dialog/GenericDialogComponent";
import { FormMode } from "../../enum/FormMode";
import {
  QuestionnaireDto,
  QuestionnaireQuestionDto,
} from "../../model/entities/questionnaire/QuestionnaireDto";
import { useApiService } from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";
import QuestionnaireQuestionFormComponent from "./QuestionnaireQuestionFormComponent";

// The gym's one health questionnaire, always open as a form: its name, and its
// questions in the order members see them. Every change is saved as it is made.
export default function QuestionnaireAdminPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const canAdd = TokenService.isUserAllowed("Questionnaires_Add");
  const canEdit = TokenService.isUserAllowed("Questionnaires_Edit");
  const canDelete = TokenService.isUserAllowed("Questionnaires_Delete");

  const [questionnaire, setQuestionnaire] = useState<QuestionnaireDto>(new QuestionnaireDto());
  const [name, setName] = useState("");
  const [editing, setEditing] = useState<QuestionnaireQuestionDto | undefined>(undefined);
  const [deleting, setDeleting] = useState<QuestionnaireQuestionDto | undefined>(undefined);
  const [isSaving, setIsSaving] = useState(false);

  const show = (response: QuestionnaireDto | null) => {
    if (!response) return;
    setQuestionnaire(response);
    setName(response.name);
  };

  useEffect(() => {
    apiService.getQuestionnaire().then(show);
  }, []);

  const saveName = async () => show(await apiService.setQuestionnaireName(name));

  const saveQuestion = async () => {
    if (!editing) return;
    setIsSaving(true);
    const response = await apiService.saveQuestionnaireQuestion(editing);
    setIsSaving(false);
    if (response) {
      show(response);
      setEditing(undefined);
    }
  };

  const deleteQuestion = async () => {
    if (!deleting) return;
    setIsSaving(true);
    const response = await apiService.deleteQuestionnaireQuestion(deleting.id);
    setIsSaving(false);
    if (response) {
      show(response);
      setDeleting(undefined);
    }
  };

  const move = async (question: QuestionnaireQuestionDto, direction: -1 | 1) =>
    show(await apiService.moveQuestionnaireQuestion(question.id, direction));

  const questions = questionnaire.questions;
  const isNameDirty = name.trim() !== questionnaire.name;

  return (
    <div
      className="mx-auto"
      style={{ maxWidth: "60rem" }}
    >
      <Card>
        <div className="flex flex-column gap-2 mb-4">
          <span className="text-sm font-semibold uppercase text-color-secondary">
            {t("Health questionnaire")}
          </span>
          <div className="flex flex-wrap align-items-center gap-2">
            <InputText
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t("Questionnaire name")}
              disabled={!canEdit}
              maxLength={200}
              className="flex-1 text-xl font-semibold"
              style={{ minWidth: "14rem" }}
            />
            {canEdit && isNameDirty && (
              <Button
                label={t("Save")}
                icon="pi pi-check"
                onClick={saveName}
              />
            )}
          </div>
        </div>

        <div className="flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
          <span className="text-color-secondary">
            {questions.length} {questions.length === 1 ? t("question") : t("questions")}
          </span>
          {canAdd && (
            <Button
              label={t("Add question")}
              icon="pi pi-plus"
              outlined
              onClick={() => setEditing(new QuestionnaireQuestionDto())}
            />
          )}
        </div>

        {questions.length === 0 ? (
          <div className="flex flex-column align-items-center gap-3 border-1 border-dashed surface-border border-round p-6 text-center">
            <i className="pi pi-list-check text-4xl text-color-secondary" />
            <span className="text-color-secondary">
              {t("No questions yet. Add the first one")}.
            </span>
          </div>
        ) : (
          <div className="border-1 surface-border border-round">
            {questions.map((question, index) => (
              <div
                key={question.id}
                className={`flex align-items-center gap-3 p-3 ${
                  index > 0 ? "border-top-1 surface-border" : ""
                }`}
              >
                <span
                  className="flex align-items-center justify-content-center border-circle bg-primary font-bold flex-none"
                  style={{ width: "2rem", height: "2rem" }}
                >
                  {question.orderNumber}
                </span>

                <div
                  className="flex-1 flex flex-column gap-1"
                  style={{ minWidth: 0 }}
                >
                  <span className="font-semibold">{question.title}</span>
                  <div className="flex flex-wrap align-items-center gap-3 text-sm text-color-secondary">
                    {question.image && (
                      <span className="flex align-items-center gap-1">
                        <i className="pi pi-image" />
                        {t("Image")}
                      </span>
                    )}
                    {question.answerPlaceholder && (
                      <span className="white-space-nowrap overflow-hidden text-overflow-ellipsis">
                        <i className="pi pi-pencil mr-1" />
                        {question.answerPlaceholder}
                      </span>
                    )}
                  </div>
                </div>

                <div className="flex align-items-center flex-none">
                  {canEdit && (
                    <>
                      <Button
                        icon="pi pi-arrow-up"
                        rounded
                        text
                        aria-label={t("Move up")}
                        disabled={index === 0}
                        onClick={() => move(question, -1)}
                      />
                      <Button
                        icon="pi pi-arrow-down"
                        rounded
                        text
                        aria-label={t("Move down")}
                        disabled={index === questions.length - 1}
                        onClick={() => move(question, 1)}
                      />
                      <Button
                        icon="pi pi-pencil"
                        rounded
                        text
                        aria-label={t("Edit")}
                        onClick={() => setEditing({ ...question })}
                      />
                    </>
                  )}
                  {canDelete && (
                    <Button
                      icon="pi pi-trash"
                      rounded
                      text
                      severity="danger"
                      aria-label={t("Delete")}
                      onClick={() => setDeleting(question)}
                    />
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      <GenericDialogComponent
        header={editing && editing.id > 0 ? t("Edit question") : t("New question")}
        visible={editing !== undefined}
        control={{ showDialog: () => {}, hideDialog: () => setEditing(undefined) }}
        formMode={FormMode.EDIT}
        footer={
          <Button
            label={t("Save")}
            icon="pi pi-check"
            loading={isSaving}
            disabled={!editing?.title.trim()}
            onClick={saveQuestion}
          />
        }
      >
        {editing ? (
          <QuestionnaireQuestionFormComponent
            question={editing}
            update={(updates) =>
              setEditing((previous) => (previous ? { ...previous, ...updates } : previous))
            }
          />
        ) : (
          <></>
        )}
      </GenericDialogComponent>

      <GenericDialogComponent
        header={`${t("Are you sure")}?`}
        visible={deleting !== undefined}
        control={{ showDialog: () => {}, hideDialog: () => setDeleting(undefined) }}
        formMode={FormMode.EDIT}
        footer={
          <Button
            label={t("Delete")}
            icon="pi pi-trash"
            severity="danger"
            loading={isSaving}
            onClick={deleteQuestion}
          />
        }
      >
        <div className="flex">
          <p className="m-0">
            {t("The answers members gave to this question will be deleted too")}.
          </p>
        </div>
      </GenericDialogComponent>
    </div>
  );
}
