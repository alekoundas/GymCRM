import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { useState } from "react";
import { UserPasswordResetDto } from "../../model/entities/user/UserPasswordResetDto";
import { useNavigate } from "react-router-dom";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import ApiService from "../../services/ApiService";
import { Divider } from "primereact/divider";
import { UserPasswordForgotDto } from "../../model/entities/user/UserPasswordForgotDto";

interface IField extends DialogChildProps {}

export default function UserPasswordForgotPage({}: IField) {
  const navigate = useNavigate();

  const [userPasswordForgotDto, setUserPasswordForgotDto] =
    useState<UserPasswordForgotDto>(new UserPasswordForgotDto());
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    setLoading(true);

    try {
      var response = await ApiService.passwordForgot(userPasswordForgotDto);
      if (response) {
        setTimeout(() => navigate("/users/login"), 3000); // Redirect to login after 3s
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <div className="flex align-items-center justify-content-center">
        <div className="surface-card p-4 shadow-2 border-round w-full lg:w-6">
          <div className="text-center mb-5">
            <div className="text-900 text-3xl font-medium mb-3">
              Forgot Password
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
            value={userPasswordForgotDto.email}
            onChange={(e) =>
              setUserPasswordForgotDto({
                ...userPasswordForgotDto,
                email: e.target.value,
              })
            }
            className="w-full mb-3"
            required
          />
          <Divider />
          <Button
            label="Send Reset Link"
            className="w-full"
            disabled={loading}
            loading={loading}
            onClick={handleSubmit}
          />
          {/* <Button
            label="Back to Login"
            className="p-button-text p-mt-2"
            onClick={() => navigate("/login")}
          /> */}
        </div>
      </div>
    </>
  );
}
