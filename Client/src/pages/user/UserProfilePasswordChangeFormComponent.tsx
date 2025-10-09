import { useUserStore } from "../../stores/UserStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { Password } from "primereact/password";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function UserProfilePasswordChangeFormComponent({
  formMode,
}: IField) {
  const { t } = useTranslator();
  const { userPasswordChangeDto, setUserPasswordChangeDto } = useUserStore();
  return (
    <>
      <div className="field">
        <label
          htmlFor="oldPassword"
          className="block text-900 font-medium mb-2"
        >
          {t("Old Password")}*
        </label>
        <Password
          id="oldPassword"
          value={userPasswordChangeDto.oldPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              oldPassword: e.target.value,
            });
          }}
          placeholder={t("Old Password")}
          toggleMask
          feedback={false}
        />
      </div>
      <div className="field">
        <label
          htmlFor="newPassword"
          className="block text-900 font-medium mb-2"
        >
          {t("New Password")}*
        </label>
        <Password
          id="newPassword"
          value={userPasswordChangeDto.newPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              newPassword: e.target.value,
            });
          }}
          placeholder={t("New Password")}
          toggleMask
          feedback={true}
        />
      </div>
      <div className="field">
        <label
          htmlFor="confirmNewPassword"
          className="block text-900 font-medium mb-2"
        >
          {t("Confirm New Password")}*
        </label>
        <Password
          id="confirmNewPassword"
          value={userPasswordChangeDto.confirmNewPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              confirmNewPassword: e.target.value,
            });
          }}
          placeholder={t("Confirm new password")}
          toggleMask
          feedback={false}
        />
      </div>
    </>
  );
}
