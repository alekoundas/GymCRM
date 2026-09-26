import { useRef } from "react";
import { Button } from "primereact/button";
import { FileUpload, FileUploadHandlerEvent } from "primereact/fileupload";
import { InputText } from "primereact/inputtext";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import RichTextAreaComponent from "../../components/core/text-area/RichTextAreaComponent";
import { QuestionnaireQuestionDto } from "../../model/entities/questionnaire/QuestionnaireDto";
import { useToast } from "../../contexts/ToastContext";
import { useTranslator } from "../../services/TranslatorService";
import { ALLOWED_IMAGE_ACCEPT, isAllowedImageType } from "../../services/ImageService";

interface IField extends DialogChildProps {
  question: QuestionnaireQuestionDto;
  update: (updates: Partial<QuestionnaireQuestionDto>) => void;
}

// Kept as a data url on the question. A few megabytes is plenty for a cropped body
// chart, and stops a phone photo at full size going into every member's page.
const MAX_IMAGE_BYTES = 3 * 1024 * 1024;

export default function QuestionnaireQuestionFormComponent({ question, update }: IField) {
  const { t } = useTranslator();
  const { showError } = useToast();
  const fileUploadRef = useRef<FileUpload>(null);

  const onImageSelected = (event: FileUploadHandlerEvent) => {
    const file = event.files[0];
    fileUploadRef.current?.clear();
    if (!file) return;

    if (!isAllowedImageType(file)) {
      showError(t("Please choose a JPG, PNG or WebP image"));
      return;
    }

    if (file.size > MAX_IMAGE_BYTES) {
      showError(t("The image is too large. Please use one under 3 MB"));
      return;
    }

    const reader = new FileReader();
    reader.onload = () => update({ image: reader.result as string });
    reader.readAsDataURL(file);
  };

  return (
    <div className="flex flex-column gap-4">
      <div className="flex flex-column gap-2">
        <label
          htmlFor="question-title"
          className="font-semibold"
        >
          {t("Title")} *
        </label>
        <InputText
          id="question-title"
          value={question.title}
          onChange={(e) => update({ title: e.target.value })}
          maxLength={300}
          className="w-full"
        />
      </div>

      <div className="flex flex-column gap-2">
        <span className="font-semibold">{t("Question")}</span>
        <RichTextAreaComponent
          value={question.details}
          isEnabled={true}
          onChange={(value) => update({ details: value })}
        />
      </div>

      <div className="flex flex-column gap-2">
        <span className="font-semibold">{t("Image")}</span>
        <span className="text-sm text-color-secondary">
          {t("Optional. For example a body chart with numbered areas the member can refer to")}.
        </span>
        {question.image ? (
          <div className="flex flex-column align-items-start gap-2">
            <img
              src={question.image}
              alt={question.title}
              className="border-round border-1 surface-border"
              style={{ maxWidth: "100%", maxHeight: "16rem", objectFit: "contain" }}
            />
            <Button
              label={t("Remove image")}
              icon="pi pi-trash"
              severity="danger"
              size="small"
              text
              onClick={() => update({ image: "" })}
            />
          </div>
        ) : (
          <FileUpload
            ref={fileUploadRef}
            mode="basic"
            accept={ALLOWED_IMAGE_ACCEPT}
            customUpload
            auto
            chooseLabel={t("Upload image")}
            uploadHandler={onImageSelected}
          />
        )}
      </div>

      <div className="flex flex-column gap-2">
        <label
          htmlFor="question-placeholder"
          className="font-semibold"
        >
          {t("Answer placeholder")}
        </label>
        <InputText
          id="question-placeholder"
          value={question.answerPlaceholder}
          onChange={(e) => update({ answerPlaceholder: e.target.value })}
          placeholder={t("e.g. 18 knee pain")}
          maxLength={300}
          className="w-full"
        />
        <span className="text-sm text-color-secondary">
          {t("Shown faded in the answer box until the member types")}.
        </span>
      </div>
    </div>
  );
}
