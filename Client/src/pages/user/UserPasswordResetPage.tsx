import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { Password } from "primereact/password";
import { useEffect, useState } from "react";
import { UserPasswordResetDto } from "../../model/entities/user/UserPasswordResetDto";
import { useNavigate, useSearchParams } from "react-router-dom";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import ApiService from "../../services/ApiService";

interface IField extends DialogChildProps {}

export default function UserPasswordResetPage({}: IField) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams(); // Url values.

  const [userPasswordResetDto, setUserPasswordResetDto] =
    useState<UserPasswordResetDto>(new UserPasswordResetDto());
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      await ApiService.passwordReset(userPasswordResetDto);
      setTimeout(() => navigate("/login"), 3000); // Redirect to login after 3s
    } finally {
      setLoading(false);
    }
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
      <div className="p-d-flex p-jc-center p-mt-5">
        <div
          className="p-card p-p-4"
          style={{ maxWidth: "400px" }}
        >
          <h2>Reset Password</h2>
          <form onSubmit={handleSubmit}>
            <div className="p-field p-mb-3">
              <label
                htmlFor="email"
                className="p-d-block"
              >
                Email
              </label>
              <InputText
                id="email"
                value={userPasswordResetDto.email}
                onChange={(e) =>
                  setUserPasswordResetDto({
                    ...userPasswordResetDto,
                    email: e.target.value,
                  })
                }
                className="p-d-block p-inputtext-lg"
                required
                type="email"
                disabled={!!searchParams.get("email")} // Disable if from URL
              />
            </div>

            <div className="p-field p-mb-3">
              <label
                htmlFor="newPassword"
                className="p-d-block"
              >
                New Password
              </label>
              <Password
                id="newPassword"
                value={userPasswordResetDto.newPassword}
                onChange={(e) =>
                  setUserPasswordResetDto({
                    ...userPasswordResetDto,
                    newPassword: e.target.value,
                  })
                }
                className="p-d-block"
                toggleMask
                feedback={true}
                required
              />
            </div>

            <div className="p-field p-mb-3">
              <label
                htmlFor="confirmNewPassword"
                className="p-d-block"
              >
                New Password
              </label>
              <Password
                id="confirmNewPassword"
                value={userPasswordResetDto.confirmNewPassword}
                onChange={(e) =>
                  setUserPasswordResetDto({
                    ...userPasswordResetDto,
                    confirmNewPassword: e.target.value,
                  })
                }
                className="p-d-block"
                toggleMask
                feedback={true}
                required
              />
            </div>
            <Button
              label="Reset Password"
              type="submit"
              className="p-button-primary p-d-block"
              disabled={
                loading ||
                !userPasswordResetDto.email ||
                !userPasswordResetDto.token ||
                !userPasswordResetDto.newPassword ||
                !userPasswordResetDto.confirmNewPassword
              }
              loading={loading}
            />
          </form>
          <Button
            label="Back to Login"
            className="p-button-text p-mt-2"
            onClick={() => navigate("/login")}
          />
        </div>
      </div>
    </>
  );
}
