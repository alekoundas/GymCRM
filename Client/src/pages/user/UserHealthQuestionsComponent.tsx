import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { InputTextarea } from "primereact/inputtextarea";
import { Tag } from "primereact/tag";
import RichTextViewComponent from "../../components/core/text-area/RichTextViewComponent";
import {
  QuestionnaireDto,
  QuestionnaireQuestionDto,
} from "../../model/entities/questionnaire/QuestionnaireDto";
import { useApiService } from "../../services/ApiService";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";

interface IField {
  userId: string | undefined;
  isAdminView: boolean;
}

const hasDetails = (question: QuestionnaireQuestionDto): boolean =>
  (!!question.details && question.details !== "<p><br></p>") || !!question.image;

// The member's answers to the gym's health questions. Read as a list - each title
// with its answer, and the full question and picture a click away - and edited all
// at once in a full-screen sheet, where any answer can be filled in, changed or
// cleared before a single save.
export default function UserHealthQuestionsComponent({ userId, isAdminView }: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const targetUserId = isAdminView ? userId : undefined;
  const canEdit = !isAdminView || TokenService.isUserAllowed("Users_Edit");

  const [questionnaire, setQuestionnaire] = useState<QuestionnaireDto>(new QuestionnaireDto());
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const [expanded, setExpanded] = useState<Record<number, boolean>>({});
  const [isLoaded, setIsLoaded] = useState(false);

  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState<Record<number, string>>({});
  const [isSaving, setIsSaving] = useState(false);

  const toRecord = (list: { questionnaireQuestionId: number; answer: string }[]) =>
    Object.fromEntries(list.map((x) => [x.questionnaireQuestionId, x.answer]));

  useEffect(() => {
    Promise.all([
      apiService.getQuestionnaire(),
      apiService.getQuestionnaireAnswers(targetUserId),
    ]).then(([questionnaireResponse, answersResponse]) => {
      if (questionnaireResponse) setQuestionnaire(questionnaireResponse);
      if (answersResponse) setAnswers(toRecord(answersResponse));
      setIsLoaded(true);
    });
  }, [userId]);

  const openEditor = () => {
    setDraft({ ...answers });
    setIsEditing(true);
  };

  const save = async () => {
    setIsSaving(true);
    const response = await apiService.saveQuestionnaireAnswers(
      questionnaire.questions.map((x) => ({
        questionnaireQuestionId: x.id,
        answer: draft[x.id] ?? "",
      })),
      targetUserId
    );
    setIsSaving(false);

    if (response) {
      setAnswers(toRecord(response));
      setIsEditing(false);
    }
  };

  const questions = questionnaire.questions;
  const answeredCount = questions.filter((x) => answers[x.id]).length;

  const number = (question: QuestionnaireQuestionDto) => (
    <span
      className="flex align-items-center justify-content-center border-circle bg-primary font-bold flex-none"
      style={{ width: "2rem", height: "2rem" }}
    >
      {question.orderNumber}
    </span>
  );

  const detailsBlock = (question: QuestionnaireQuestionDto) => (
    <div className="flex flex-column gap-2">
      <RichTextViewComponent value={question.details} />
      {question.image && (
        <img
          src={question.image}
          alt={question.title}
          className="border-round border-1 surface-border"
          style={{ maxWidth: "100%", maxHeight: "24rem", objectFit: "contain", alignSelf: "flex-start" }}
        />
      )}
    </div>
  );

  if (!isLoaded) return <></>;

  if (questions.length === 0)
    return (
      <div className="flex flex-column align-items-center gap-3 border-1 border-dashed surface-border border-round p-6 text-center text-color-secondary">
        <i className="pi pi-heart text-4xl" />
        <span>{t("There are no health questions yet")}.</span>
      </div>
    );

  return (
    <>
      <div className="flex flex-wrap align-items-center justify-content-between gap-3 mb-3">
        <div className="flex flex-column gap-1">
          <span className="text-xl font-semibold">
            {questionnaire.name || t("Health questions")}
          </span>
          <span className="text-sm text-color-secondary">
            {answeredCount} / {questions.length} {t("answered")}
          </span>
        </div>
        {canEdit && (
          <Button
            label={t("Edit answers")}
            icon="pi pi-pencil"
            onClick={openEditor}
          />
        )}
      </div>

      <div className="flex flex-column gap-2">
        {questions.map((question) => {
          const answer = answers[question.id];
          const isOpen = !!expanded[question.id];

          return (
            <div
              key={question.id}
              className="flex align-items-start gap-3 border-1 surface-border border-round p-3"
            >
              {number(question)}

              <div
                className="flex-1 flex flex-column gap-2"
                style={{ minWidth: 0 }}
              >
                <span className="font-semibold">{question.title}</span>

                {isOpen && detailsBlock(question)}

                {answer ? (
                  <span style={{ whiteSpace: "pre-line" }}>{answer}</span>
                ) : (
                  <Tag
                    severity="secondary"
                    value={t("Not answered")}
                    className="align-self-start"
                  />
                )}
              </div>

              {hasDetails(question) && (
                <Button
                  icon={isOpen ? "pi pi-chevron-up" : "pi pi-chevron-down"}
                  rounded
                  text
                  aria-label={isOpen ? t("Hide question") : t("Show question")}
                  onClick={() =>
                    setExpanded((previous) => ({ ...previous, [question.id]: !isOpen }))
                  }
                />
              )}
            </div>
          );
        })}
      </div>

      <Dialog
        visible={isEditing}
        onHide={() => setIsEditing(false)}
        maximized
        draggable={false}
        resizable={false}
        blockScroll
        header={questionnaire.name || t("Health questions")}
        footer={
          <div className="flex justify-content-end">
            <Button
              label={t("Save")}
              icon="pi pi-check"
              loading={isSaving}
              onClick={save}
            />
          </div>
        }
      >
        <div
          className="mx-auto flex flex-column gap-4 py-2"
          style={{ maxWidth: "48rem" }}
        >
          {questions.map((question) => (
            <div
              key={question.id}
              className="flex align-items-start gap-3"
            >
              {number(question)}
              <div
                className="flex-1 flex flex-column gap-2"
                style={{ minWidth: 0 }}
              >
                <label
                  htmlFor={`answer-${question.id}`}
                  className="font-semibold"
                >
                  {question.title}
                </label>
                {detailsBlock(question)}
                <InputTextarea
                  id={`answer-${question.id}`}
                  value={draft[question.id] ?? ""}
                  onChange={(e) =>
                    setDraft((previous) => ({ ...previous, [question.id]: e.target.value }))
                  }
                  placeholder={question.answerPlaceholder}
                  autoResize
                  rows={2}
                  maxLength={2000}
                  className="w-full"
                />
              </div>
            </div>
          ))}
        </div>
      </Dialog>
    </>
  );
}
