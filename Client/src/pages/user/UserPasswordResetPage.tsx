import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { Password } from "primereact/password";
import { useEffect, useState } from "react";
import { UserPasswordResetDto } from "../../model/entities/user/UserPasswordResetDto";
import { useNavigate, useSearchParams } from "react-router-dom";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

interface IField extends DialogChildProps {}

export default function UserPasswordResetPage({}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams(); // Url values.

  const [userPasswordResetDto, setUserPasswordResetDto] =
    useState<UserPasswordResetDto>(new UserPasswordResetDto());
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    setLoading(true);

    const response = await apiService.passwordReset(userPasswordResetDto);
    if (response) {
      setTimeout(() => navigate("/users/login"), 3000); // Redirect to login after 3s
    }
    setLoading(false);
  };

  useEffect(() => {
    const email = searchParams.get("email");
    const token = searchParams.get("token");
    if (email && token)
      setUserPasswordResetDto({
        ...userPasswordResetDto,
        email: email,
        token: token,
      });
  }, []);

  return (
    <>
      <div className="flex align-items-center justify-content-center">
        <div className="surface-card p-4 shadow-2 border-round w-full lg:w-6">
          <div className="text-center mb-5">
            <div className="text-900 text-3xl font-medium mb-3">
              {t("Reset Password")}
            </div>
          </div>

          <label
            htmlFor="email"
            className="p-d-block"
          >
            Email
          </label>
          <InputText
            id="email"
            name="email"
            type="email"
            placeholder="Email"
            value={userPasswordResetDto.email}
            onChange={(e) =>
              setUserPasswordResetDto({
                ...userPasswordResetDto,
                email: e.target.value,
              })
            }
            className="w-full mb-3"
            disabled={!!searchParams.get("email")}
          />

          <label
            htmlFor="newPassword"
            className="p-d-block"
          >
            {t("New Password")}
          </label>
          <Password
            id="newPassword"
            name="newPassword"
            placeholder={t("Password")}
            value={userPasswordResetDto.newPassword}
            onChange={(e) =>
              setUserPasswordResetDto({
                ...userPasswordResetDto,
                newPassword: e.target.value,
              })
            }
            className="w-full mb-3"
            toggleMask
            feedback={true}
            required
          />

          <div className="p-field p-mb-3">
            <label
              htmlFor="confirmNewPassword"
              className="p-d-block"
            >
              {t("New Password")}
            </label>
            <Password
              id="confirmNewPassword"
              name="confirmNewPassword"
              placeholder={t("Password")}
              value={userPasswordResetDto.confirmNewPassword}
              onChange={(e) =>
                setUserPasswordResetDto({
                  ...userPasswordResetDto,
                  confirmNewPassword: e.target.value,
                })
              }
              className="w-full mb-3"
              toggleMask
              feedback={true}
              required
            />
          </div>
          <Button
            label={t("Reset Password")}
            className="p-button-primary p-d-block"
            disabled={
              loading ||
              !userPasswordResetDto.email ||
              !userPasswordResetDto.token ||
              !userPasswordResetDto.newPassword ||
              !userPasswordResetDto.confirmNewPassword
            }
            loading={loading}
            onClick={handleSubmit}
          />
          <Button
            label={t("Back to Login")}
            className="p-button-text p-mt-2"
            onClick={() => navigate("/users/login")}
          />
        </div>
      </div>
    </>
  );
}
