import { InputTextarea } from "primereact/inputtextarea";
import { Button } from "primereact/button";
import { useEffect, useState } from "react";
import { useUserStore } from "../../stores/UserStore";
import { UserDto } from "../../model/entities/user/UserDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function UserMedicalHistoryComponent() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { userDto, updateUserDto } = useUserStore();

  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [text, setText] = useState(userDto?.medicalHistory ?? "");

  // The profile loads after this mounts, so the box has to follow the store.
  useEffect(() => {
    setText(userDto?.medicalHistory ?? "");
  }, [userDto]);

  const handleCancel = () => {
    setText(userDto?.medicalHistory ?? "");
    setIsEditing(false);
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      const response = await apiService.update<UserDto>(
        "Users",
        { ...userDto, medicalHistory: text },
        userDto.id,
      );

      if (response) {
        updateUserDto(response);
        setIsEditing(false);
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex flex-column gap-3">
      <div className="flex flex-wrap align-items-center justify-content-between gap-2">
        <span className="text-color-secondary">
          {t("Anything the trainer should know before a session")}
        </span>

        <div className="flex gap-2">
          {isEditing ? (
            <>
              <Button
                label={t("Cancel")}
                icon="pi pi-times"
                text
                onClick={handleCancel}
              />
              <Button
                label={t("Save")}
                icon="pi pi-check"
                loading={isSaving}
                onClick={handleSave}
              />
            </>
          ) : (
            <Button
              label={t("Edit")}
              icon="pi pi-pencil"
              outlined
              onClick={() => setIsEditing(true)}
            />
          )}
        </div>
      </div>

      <InputTextarea
        id="medicalHistory"
        placeholder={t("Medical History")}
        className="w-full"
        // Room for a real history rather than a line or two. It is the whole point
        // of the tab, so it fills it.
        style={{ minHeight: "60vh" }}
        value={text}
        onChange={(e) => setText(e.target.value)}
        disabled={!isEditing}
      />
    </div>
  );
}
